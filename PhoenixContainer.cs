using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Phoenix.DI
{
    [Export(typeof(IPhoenixContainer), CreationPolicy.Shared)]
    public sealed class PhoenixContainer : IPhoenixContainer
    {
        #region [ Fields & Properties ]

        private readonly Dictionary<Type, Type> _typeToImplementationMap = new Dictionary<Type, Type>();

        private InstancesList _instanceList = new InstancesList();

        #endregion [ Fields & Properties ]

        #region [ Constructor ]

        public PhoenixContainer()
        {
            RegisterContainer(this);
        }

        #endregion [ Constructor ]

        #region [ Public Methods ]

        #region [ Register Methods ]

        public void RegisterType<TType, TImplementation>(Export exportAttribute = null)
        {
            var typeImplementation = typeof(TImplementation);
            var typeType = typeof(TType);

            RegisterType(typeType, typeImplementation, exportAttribute);

        }

        public void RegisterType<TType>(Export expAttribute = null)
        {
            RegisterType<TType, TType>(expAttribute);
        }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass || (type.IsValueType
                                        && !type.IsPrimitive
                                        && type.Namespace != null
                                        && !type.Namespace.StartsWith("System.")))
                {
                    Attribute[] attrs = Attribute.GetCustomAttributes(type);

                    var _exportAttribute = attrs.Where(a => a.GetType() == typeof(Export)).FirstOrDefault() as Export;

                    if (_exportAttribute != null)
                    {
                        RegisterType(_exportAttribute.ContractType, type);
                    }
                }
            }
        }

        public void RegisterInstance<TType>(object objInstance)
        {
            var typeContract = typeof(TType);

            if (!IsTypeImplementationExists(typeContract))
            {
                _typeToImplementationMap.Add(typeContract, typeContract);
                _instanceList.Add(typeContract, typeContract);
            }

            var typeImplementation = _typeToImplementationMap[typeContract];

            _instanceList.SetInstance(typeContract, typeImplementation, objInstance);
        }


        #endregion [ Register Methods ]

        #region [ Resolve Methods ]

        public T Resolve<T>(ConstructorParam[] paramsList = null)
        {
            var typeToResolve = typeof(T);
            var returnObject = (T)GetInstance(typeToResolve, paramsList);

            return returnObject;
        }

        #endregion [ Resolve Methods ]

        #endregion [ Public Methods ]

        #region [ Private Methods ]

        #region [ Register Methods ]

        private void RegisterType(Type typeContract, Type typeImplementation, Export exportAttribute = null)
        {

            HandleExportAttribute(typeContract, typeImplementation, exportAttribute);

            if (!IsTypeImplementationExists(typeContract))
                _typeToImplementationMap.Add(typeContract, typeImplementation);
        }

        private void HandleExportAttribute(Type typeContract, Type typeImplementation, Export exportAttribute = null)
        {
            Export _exportAttribute = null;

            if (exportAttribute != null)
            {
                _exportAttribute = exportAttribute;
            }
            else
            {
                Attribute[] attrs = Attribute.GetCustomAttributes(typeImplementation);

                _exportAttribute = attrs.Where(a => a.GetType() == typeof(Export)).FirstOrDefault() as Export;
            }

            if (_exportAttribute == null && !_instanceList.IsSharedType(typeContract, typeImplementation))
            {
                _instanceList.Add(typeContract, typeImplementation);
            }
            else if (_exportAttribute != null
                        && _exportAttribute.CreationPolicy == CreationPolicy.Shared
                        && !_instanceList.IsSharedType(_exportAttribute.ContractType, typeImplementation))
            {
                _instanceList.Add(_exportAttribute.ContractType, typeImplementation);
            }
        }

        private void RegisterContainer(PhoenixContainer container)
        {
            RegisterType<IPhoenixContainer, PhoenixContainer>();
            _instanceList.SetInstance(typeof(IPhoenixContainer), typeof(PhoenixContainer), container);
        }

        #endregion [ Register Methods ]

        #region [ Resolve Methods ]

        private object GetInstance(Type type, ConstructorParam[] paramsList = null)
        {
            Type resolvedType = LookUpTypeImplementation(type);

            ConstructorInfo constructor = null;

            /*
             * Some Error Checking here,
             * A Class should not have more than 1 Importing Constructor
             */

            if (resolvedType.GetConstructors().Where(a => a.GetCustomAttributes(typeof(ImportingConstructor), false).Any()).Count() > 1)
            {
                string message = string.Format("ERROR # Type {0} must not have multiple Importing Constructors.", resolvedType.ToString());
                Debug.WriteLine(message);
                throw new EntryPointNotFoundException(message);
            }

            var importingConstructor = resolvedType.GetConstructors().Where(a => a.GetCustomAttributes(typeof(ImportingConstructor), false).Any()).FirstOrDefault();
            if (importingConstructor != null)
                constructor = importingConstructor;
            else
                constructor = resolvedType.GetConstructors().FirstOrDefault();

            List<ParameterInfo> constructorParameters = new List<ParameterInfo>();
            if (constructor != null)
                constructorParameters = constructor.GetParameters().ToList();

            if (!constructorParameters.Any())
            {

                var returnObject = _instanceList.GetInstance(type, resolvedType);

                if (returnObject == null)
                {
                    returnObject = Activator.CreateInstance(resolvedType);

                    if (_instanceList.IsSharedType(type, resolvedType))
                        _instanceList.SetInstance(type, resolvedType, returnObject);

                    bool _noErrors = ResolveProperties(returnObject, resolvedType);

                    if (typeof(INofityImportsCompleted).IsAssignableFrom(resolvedType))
                    {
                        (returnObject as INofityImportsCompleted).OnImportCompleted(_noErrors);
                    }


                }

                return returnObject;

            }
            else
            {
                var returnObject = _instanceList.GetInstance(type, resolvedType);

                if (returnObject == null)
                {
                    var constructorParmaObjectsList = GetConstructorParametersInstances(constructorParameters, paramsList).ToArray();

                    returnObject = constructor.Invoke(constructorParmaObjectsList.ToArray());

                    if (_instanceList.IsSharedType(type, resolvedType))
                        _instanceList.SetInstance(type, resolvedType, returnObject);

                    bool _noErrors = ResolveProperties(returnObject, resolvedType);

                    if (typeof(INofityImportsCompleted).IsAssignableFrom(resolvedType))
                    {
                        (returnObject as INofityImportsCompleted).OnImportCompleted(_noErrors);
                    }

                }

                return returnObject;
            }

        }

        private bool IsTypeImplementationExists(Type type)
        {
            try
            {
                var implementation = _typeToImplementationMap[type];
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Type LookUpTypeImplementation(Type type)
        {
            try
            {
                return _typeToImplementationMap[type];
            }
            catch (Exception)
            {
                string message = string.Format("ERROR # Type {0} could not be resolved, because it was not Registered.", type.ToString());
                Debug.WriteLine(message);
                throw new InvalidOperationException(message);
            }
        }

        private IEnumerable<object> GetConstructorParametersInstances(IEnumerable<ParameterInfo> parameters, ConstructorParam[] paramsList = null)
        {
            return parameters.Select(p => ResolveConstructorParameter(p, paramsList)).ToList();
        }

        private object ResolveConstructorParameter(ParameterInfo p, ConstructorParam[] paramsList = null)
        {
            if (paramsList != null)
            {
                var paramObjectFromList = paramsList.Where(a => a.Param == p.Name).FirstOrDefault();

                if (paramObjectFromList != null)
                    return paramObjectFromList.Object;
                else
                    return GetInstance(p.ParameterType);
            }
            else
                return GetInstance(p.ParameterType);
        }

        private bool ResolveProperties(object obj, Type typeOfObject)
        {
            bool _noErrors = true;

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

            ///Resolve Properties first
            foreach (var property in typeOfObject.GetProperties(flags))
            {
                Debug.WriteLine(string.Format("{0} ", property.Name));

                Attribute[] attrs = Attribute.GetCustomAttributes(property);

                if (attrs.Any() && attrs.Where(a => a.GetType() == typeof(Import)).Any())
                {
                    object propertyValue = null;
                    try
                    {
                        propertyValue = property.GetValue(obj, null);
                    }
                    catch (Exception)
                    {
                        _noErrors = false;
                        string message = string.Format("ERROR # Property {0} of {1} could not be accessed due to its protection level.", property.Name.ToString(), typeOfObject.ToString());
                        Debug.WriteLine(message);
                        continue;
                        //throw new InvalidOperationException(message);
                    }
                    if (propertyValue == null)
                    {
                        var valueOfProperty = GetInstance(property.PropertyType);

                        try
                        {
                            property.SetValue(obj, valueOfProperty, null);
                        }
                        catch (Exception)
                        {
                            _noErrors = false;
                            string message = string.Format("ERROR # Property {0} of {1} could not be accessed due to its protection level.", property.Name.ToString(), typeOfObject.ToString());
                            Debug.WriteLine(message);
                            //throw new InvalidOperationException(message);
                        }

                    }
                }

            }

            return _noErrors;

        }

        #endregion [ Resolve Methods ]






        #endregion [ Private Methods ]

    }

    #region [ Internal Classes ]

    internal sealed class InstancesList
    {
        private List<Instance> _typesInstances = new List<Instance>();

        public void Add(Type typeContract, Type typeInstance)
        {
            var instance = new Instance { TypeOfContract = typeContract, TypeOfInstance = typeInstance, Instantiated = false, ObjectOfInstance = null };

            _typesInstances.Add(instance);
        }

        public bool IsSharedType(Type typeContract, Type typeInstance)
        {
            var instance = _typesInstances.Where(a => a.TypeOfContract == typeContract && a.TypeOfInstance == typeInstance).FirstOrDefault();

            if (instance == null)
                return false;
            else
                return true;
        }

        public object GetInstance(Type typeContract, Type typeInstance)
        {
            var instance = _typesInstances.Where(a => a.TypeOfContract == typeContract && a.TypeOfInstance == typeInstance).FirstOrDefault();

            if (instance == null)
                return null;
            else
                return instance.ObjectOfInstance;
        }

        public void SetInstance(Type typeContract, Type typeInstance, Object obj)
        {
            var instance = _typesInstances.Where(a => a.TypeOfContract == typeContract && a.TypeOfInstance == typeInstance).FirstOrDefault();

            instance.Instantiated = true;
            instance.ObjectOfInstance = obj;
        }

    }

    internal sealed class Instance
    {
        public Type TypeOfContract { get; set; }
        public Type TypeOfInstance { get; set; }
        public Object ObjectOfInstance { get; set; }
        public bool Instantiated { get; set; }
    }

    #endregion [ Internal Classes ]


}

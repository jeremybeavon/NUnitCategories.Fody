using System;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;

namespace NUnitCategories.Fody
{
    public class ModuleWeaver
    {
        private readonly Lazy<MethodDefinition> categoryAttributeConstructor;

        public ModuleDefinition ModuleDefinition { get; set; }

        public Action<string> LogError { get; set; }

        public Action<string> LogWarning { get; set; }

        public Action<string> LogDebug { get; set; }

        public XElement Config { get; set; }

        public ModuleWeaver()
        {
            categoryAttributeConstructor = new Lazy<MethodDefinition>(FindCategoryAttributeConstructor);
            LogError = message => { };
            LogWarning = message => { };
            LogDebug = message => { };
        }

        public void Execute()
        {
            int? processorCount = ReadProcessorCount();
            int testCount = 0;
            foreach (TypeDefinition type in ModuleDefinition.Types.Where(type => HasAttribute(type, "TestFixtureAttribute")))
            {
                string typeName = type.Namespace + "." + type.Name;
                AddCategoryAttribute(type, type.Namespace);
                AddCategoryAttribute(type, typeName);
                foreach (MethodDefinition method in type.Methods.Where(method => HasAttribute(method, "TestAttribute")))
                {
                    AddCategoryAttribute(method, typeName + "." + method.Name);
                }

                if (processorCount.HasValue)
                {
                    AddCategoryAttribute(type, "CPU_" + (testCount++ % processorCount.Value + 1));
                }
            }
        }

        private static bool HasAttribute(ICustomAttributeProvider provider, string attributeName)
        {
            return provider.CustomAttributes.Any(attribute => attribute.AttributeType.Name == attributeName);
        }

        private int? ReadProcessorCount()
        {
            if (Config == null)
            {
                LogDebug("ModuleWeaver.Config is null");
                return null;
            }

            XAttribute attribute = Config.Attribute("ProcessorCount");
            if (attribute == null)
            {
                LogDebug("Fody configuration for NUnitCategories does not contain a ProcessorCount attribute.");
                return null;
            }

            int processorCount;
            if (!int.TryParse(attribute.Value, out processorCount))
            {
                LogWarning("Fody configuration for NUnitCategories contains a ProcessorCount attribute but it is not an integer.");
                return null;
            }

            return processorCount;
        }

        private void AddCategoryAttribute(ICustomAttributeProvider provider, string categoryName)
        {
            MethodDefinition constructor = categoryAttributeConstructor.Value;
            if (constructor == null)
            {
                return;
            }

            CustomAttribute categoryAttribute = new CustomAttribute(ModuleDefinition.Import(constructor));
            categoryAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, categoryName));
            provider.CustomAttributes.Add(categoryAttribute);
        }

        private MethodDefinition FindCategoryAttributeConstructor()
        {
            AssemblyDefinition nunitAssembly = ModuleDefinition.AssemblyResolver.Resolve("nunit.framework");
            if (nunitAssembly == null)
            {
                LogError("nunit.framework could not be found.");
                return null;
            }

            TypeDefinition categoryAttributeType = nunitAssembly.MainModule.Types.FirstOrDefault(
                type => type.Name == "CategoryAttribute");
            if (categoryAttributeType == null)
            {
                LogError("CategoryAttribute type could not be found.");
                return null;
            }

            MethodDefinition constructor = categoryAttributeType.Methods.FirstOrDefault(
                method => method.IsConstructor && method.Parameters.Count == 1);
            if (constructor == null)
            {
                LogError("CategoryAttribute constructor could not be found.");
            }

            return constructor;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace NUnitCategories.Fody
{
    public class ModuleWeaver
    {
        private readonly Lazy<MethodDefinition> categoryAttributeConstructor;

        public ModuleDefinition ModuleDefinition { get; set; }

        public Action<string> LogError { get; set; }

        public ModuleWeaver()
        {
            categoryAttributeConstructor = new Lazy<MethodDefinition>(FindCategoryAttributeConstructor);
            LogError = message => { };
        }

        public void Execute()
        {
            foreach (TypeDefinition type in ModuleDefinition.Types.Where(type => HasAttribute(type, "TestFixtureAttribute")))
            {
                string typeName = type.Namespace + "." + type.Name;
                AddCategoryAttribute(type, type.Namespace);
                AddCategoryAttribute(type, typeName);
                foreach (MethodDefinition method in type.Methods.Where(method => HasAttribute(method, "TestAttribute")))
                {
                    AddCategoryAttribute(method, typeName + "." + method.Name);
                }
            }
        }

        private static bool HasAttribute(ICustomAttributeProvider provider, string attributeName)
        {
            return provider.CustomAttributes.Any(attribute => attribute.AttributeType.Name == attributeName);
        }

        private void AddCategoryAttribute(ICustomAttributeProvider provider, string categoryName)
        {
            CustomAttribute categoryAttribute = new CustomAttribute(ModuleDefinition.Import(categoryAttributeConstructor.Value));
            categoryAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, categoryName));
            provider.CustomAttributes.Add(categoryAttribute);
        }

        private MethodDefinition FindCategoryAttributeConstructor()
        {
            AssemblyDefinition nunitAssembly = ModuleDefinition.AssemblyResolver.Resolve("nunit.framework");
            if (nunitAssembly == null)
            {
                LogError("nunit.framework could not be found.");
            }

            TypeDefinition categoryAttributeType = nunitAssembly.MainModule.Types.FirstOrDefault(
                type => type.Name == "CategoryAttribute");
            if (categoryAttributeType == null)
            {
                LogError("CategoryAttribute type could not be found.");
            }

            MethodDefinition categoryAttributeConstructor = categoryAttributeType.Methods.FirstOrDefault(
                method => method.IsConstructor && method.Parameters.Count == 1);
            if (categoryAttributeConstructor == null)
            {
                LogError("CategoryAttribute constructor could not be found.");
            }

            return categoryAttributeConstructor;
        }
    }
}

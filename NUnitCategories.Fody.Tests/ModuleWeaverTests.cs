using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Mono.Cecil;
using NUnit.Framework;

namespace NUnitCategories.Fody.Tests
{
    [TestFixture]
    public class ModuleWeaverTests
    {
        private Assembly assembly;

        [TestFixtureSetUp]
        public void Setup()
        {
            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
            string assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            string newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
            File.Copy(assemblyPath, newAssemblyPath, true);

            var moduleDefinition = ModuleDefinition.ReadModule(newAssemblyPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
            moduleDefinition.Write(newAssemblyPath);

            assembly = Assembly.LoadFile(newAssemblyPath);
        }

        [Test]
        public void TestCategoryAttributeIsAddedToTestFixture1()
        {
            Type testFixture1Type = assembly.GetType("AssemblyToProcess.TestFixture1");
            CategoryAttribute attribute = (CategoryAttribute)Attribute.GetCustomAttribute(testFixture1Type, typeof(CategoryAttribute));
            attribute.Should().NotBeNull();
            attribute.Name.Should().Be("AssemblyToProcess.TestFixture1");
        }

        [Test]
        public void TestCategoryAttributeIsAddedToTestMethod1()
        {
            MethodInfo testMethod1 = assembly.GetType("AssemblyToProcess.TestFixture1").GetMethod("TestMethod1");
            CategoryAttribute attribute = (CategoryAttribute)Attribute.GetCustomAttribute(testMethod1, typeof(CategoryAttribute));
            attribute.Should().NotBeNull();
            attribute.Name.Should().Be("AssemblyToProcess.TestFixture1.TestMethod1");
        }
    }
}

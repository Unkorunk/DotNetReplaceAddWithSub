using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DotNetReplaceAddWithSub
{
    internal static class Program
    {
        private static void ReplaceAddWithSub(Stream input, Stream output)
        {
            var additionMethodInfo =
                typeof(decimal).GetMethod("op_Addition", BindingFlags.Static | BindingFlags.Public);
            var subtractionMethodInfo =
                typeof(decimal).GetMethod("op_Subtraction", BindingFlags.Static | BindingFlags.Public);

            var module = ModuleDefinition.ReadModule(input);
            var subtractionMethodReference = module.ImportReference(subtractionMethodInfo);

            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods.Where(it => it.HasBody))
                {
                    var processor = method.Body.GetILProcessor();

                    var oldInstructions = processor.Body.Instructions.Where(it => it.OpCode == OpCodes.Add).ToList();
                    foreach (var oldInstruction in oldInstructions)
                    {
                        var newInstruction = processor.Create(OpCodes.Sub);
                        processor.Replace(oldInstruction, newInstruction);
                    }

                    if (additionMethodInfo != null)
                    {
                        oldInstructions = processor.Body.Instructions.Where(it =>
                            it.OpCode == OpCodes.Call && it.Operand is MethodReference methodReference &&
                            methodReference.Name == additionMethodInfo.Name
                        ).ToList();
                        foreach (var oldInstruction in oldInstructions)
                        {
                            var newInstruction = processor.Create(OpCodes.Call, subtractionMethodReference);
                            processor.Replace(oldInstruction, newInstruction);
                        }
                    }
                }
            }

            module.Write(output);
        }

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("two arguments expected");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("wrong input file");
                return;
            }

            var inputStream = new FileStream(args[0], FileMode.Open);
            var outputStream = new FileStream(args[1], FileMode.Create);

            if (!inputStream.CanRead)
            {
                Console.WriteLine("file read error");
                return;
            }

            if (!outputStream.CanWrite)
            {
                Console.WriteLine("file write error");
                return;
            }

            ReplaceAddWithSub(inputStream, outputStream);

            inputStream.Close();
            outputStream.Close();
        }
    }
}
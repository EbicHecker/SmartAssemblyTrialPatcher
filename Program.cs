using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

var module = ModuleDefinition.FromFile(args[0]);
foreach (var instruction in (module.ManagedEntrypointMethod?.CilMethodBody ?? throw new NotSupportedException()).Instructions)
{
    if (instruction.Operand is CilInstructionLabel
        {
            Instruction:
            {
                OpCode.Code: CilCode.Call,
                Operand: MethodDefinition { Signature: { ReturnsValue: true } signature } method
            }
        } && signature.ReturnType == module.CorLibTypeFactory.Boolean && (method.CilMethodBody!.LocalVariables.Count is 1 || method.CilMethodBody!.Instructions.Any(x => x.OpCode.Code is CilCode.Call)))
    {
        method.CilMethodBody = new CilMethodBody(method)
        {
            Instructions =
            {
                new CilInstruction(CilOpCodes.Ldc_I4_1),
                new CilInstruction(CilOpCodes.Ret)
            }
        };
        
        Console.WriteLine($"patched: {method} - ({method.MetadataToken})");
        break;
    }
}

module.Write(args[0].Insert(args[0].Length - 4, "-no_trial"),
    new ManagedPEImageBuilder(MetadataBuilderFlags.PreserveAll));

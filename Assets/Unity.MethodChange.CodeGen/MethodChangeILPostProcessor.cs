using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

public class MethodChangeILPostProcessor : ILPostProcessor
{
    public override ILPostProcessor GetInstance() => this;

    public override bool WillProcess(ICompiledAssembly compiledAssembly)
    {
        // TestPackage飲みを対象とする
        return compiledAssembly.Name == "TestPackage";
    }

    public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
    {
        if (!WillProcess(compiledAssembly)) return null;

        var diagnostics = new List<DiagnosticMessage>(); // コンパイル時のログを追加できるが、今回は使わない
        using var assemblyDefinition = ILPostProcessUtility.AssemblyDefinitionFor(compiledAssembly); // AssemblyDefinitionロード

        foreach (var typeDefinition in assemblyDefinition.MainModule.Types)
        {
            if (typeDefinition.Name != "SomeComponent") continue; // SomeComponentクラスのみが対象
            foreach (var methodDefinition in typeDefinition.Methods)
            {
                // HelloメソッドとSumメソッドを見つけたら、それに対応するChangeUtilityのMethodInfoを取得する
                var replaceMethod = methodDefinition.Name switch
                {
                    "Hello" => typeof(ChangeUtility).GetMethod("Hello", BindingFlags.Static | BindingFlags.Public),
                    "Sum" => typeof(ChangeUtility).GetMethod("Sum", BindingFlags.Static | BindingFlags.Public),
                    _ => null
                };

                if (replaceMethod != null)
                {
                    ReplaceMethod(assemblyDefinition, methodDefinition, replaceMethod);
                }
            }
        }

        return ILPostProcessUtility.GetResult(assemblyDefinition, diagnostics);
    }

    /// <summary>
    /// メソッドの中で別のメソッドを呼ぶことでメソッドの実装を置き換える
    /// </summary>
    private static void ReplaceMethod(AssemblyDefinition assemblyDefinition, MethodDefinition methodDefinition, MethodInfo method)
    {
        var processor = methodDefinition.Body.GetILProcessor();
        // メソッドの実装のILの先頭を取得
        var first = processor.Body.Instructions[0];

        // 引数ロードのILコードの追加（この実装では、引数の数が4つまでに対応している）
        for (var i = 0; i < method.GetParameters().Length; i++)
        {
            var code = i switch
            {
                0 => OpCodes.Ldarg_0,
                1 => OpCodes.Ldarg_1,
                2 => OpCodes.Ldarg_2,
                3 => OpCodes.Ldarg_3,
                _ => OpCodes.Nop // 未対応
            };
            processor.InsertBefore(first, processor.Create(code));
        }

        // メソッド呼び出しに必要なMethodReferenceはModuleDefinitionのImportRefenreceメソッドで取得可能
        var methodReference = assemblyDefinition.MainModule.ImportReference(method);
        // メソッド呼び出しのILコードの追加
        processor.InsertBefore(first, processor.Create(OpCodes.Call, methodReference));
        // returnのILコードの追加
        processor.InsertBefore(first, processor.Create(OpCodes.Ret));
    }
}

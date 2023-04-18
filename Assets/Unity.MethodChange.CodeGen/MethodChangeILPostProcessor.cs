using System.Collections.Generic;
using System.Linq;
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
        // TestPackageのみを対象とする
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
                // HelloメソッドとSumメソッドを見つけたら、それに対応するMethodDefinitionを取得する
                var methodName = methodDefinition.Name;
                if (methodName == "Hello" || methodName == "Sum")
                {
                    var replaceMethod = assemblyDefinition.MainModule.GetType(typeof(ChangeUtility).FullName).Methods.First(x => x.Name == methodName);
                    ReplaceMethod(methodDefinition, replaceMethod);
                }
            }
        }

        return ILPostProcessUtility.GetResult(assemblyDefinition, diagnostics);
    }

    /// <summary>
    /// メソッドの中で別のメソッドを呼ぶことでメソッドの実装を置き換える
    /// </summary>
    private static void ReplaceMethod(MethodDefinition methodDefinition, MethodDefinition replaceMethod)
    {
        var processor = methodDefinition.Body.GetILProcessor();
        // メソッドの実装のILの先頭を取得
        var first = processor.Body.Instructions[0];

        // 引数ロードのILコードの追加
        for (var i = 0; i < replaceMethod.Parameters.Count; i++)
        {
            var argNum = i;
            if (replaceMethod.IsStatic == false)
            {
                // instanceメソッドではarg_0にthisが入るのでその分をずらす
                argNum++;
            }

            var code = argNum switch
            {
                0 => OpCodes.Ldarg_0,
                1 => OpCodes.Ldarg_1,
                2 => OpCodes.Ldarg_2,
                3 => OpCodes.Ldarg_3,
                _ => OpCodes.Ldarg_S
            };
            var instruction = code == OpCodes.Ldarg_S
                ? processor.Create(code, methodDefinition.Parameters[i].Resolve())
                : processor.Create(code);
            processor.InsertBefore(first, instruction);
        }

        // メソッド呼び出しのILコードの追加
        processor.InsertBefore(first, processor.Create(OpCodes.Call, replaceMethod));
        // returnのILコードの追加
        processor.InsertBefore(first, processor.Create(OpCodes.Ret));
    }
}

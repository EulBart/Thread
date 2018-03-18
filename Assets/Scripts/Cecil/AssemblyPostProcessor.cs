using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEngine;




public static class AssemblyPostProcessor
{
    [MenuItem("Test/PostProcess")]
    public static void PostProcess()
    {
        try
        {
            // Lock assemblies while they may be altered
            EditorApplication.LockReloadAssemblies();

            foreach( System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies() )
            {
                // Only process assemblies which are in the project
                if( assembly.Location.Replace( '\\', '/' ).StartsWith( Application.dataPath.Substring( 0, Application.dataPath.Length - 7 ) ) )
                {
                    AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly( assembly.Location );
                    AssemblyPostProcessor.PostProcessAssembly( assemblyDefinition );
                }
            }
   
            // Unlock now that we're done
            EditorApplication.UnlockReloadAssemblies();
        }
        catch( Exception e )
        {
            Debug.LogWarning( e );
        }
    }

    private static void PostProcessAssembly( AssemblyDefinition assemblyDefinition )
    {
        foreach( ModuleDefinition moduleDefinition in assemblyDefinition.Modules )
        {
            foreach( TypeDefinition typeDefinition in moduleDefinition.Types )
            {
                foreach( MethodDefinition methodDefinition in typeDefinition.Methods )
                {
                    CustomAttribute logAttribute = null;

                    foreach( CustomAttribute customAttribute in methodDefinition.CustomAttributes )
                    {
                        if( customAttribute.AttributeType.FullName == "LogAttribute" )
                        {
                            // Process method here...

                            logAttribute = customAttribute;

                            MethodReference logMethodReference = 
                                moduleDefinition.ImportReference(typeof( Debug ).GetMethod( "Log", new Type[] { typeof( object ) } ) );
                                
//                                moduleDefinition.Import( typeof( Debug ).GetMethod( "Log", new Type[] { typeof( object ) } ) );

                            ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();

                            Instruction first = methodDefinition.Body.Instructions[0];
                            ilProcessor.InsertBefore( first, Instruction.Create( OpCodes.Ldstr, 
                                "Enter " + typeDefinition.FullName + "." + methodDefinition.Name ) );
                            ilProcessor.InsertBefore( first, Instruction.Create( OpCodes.Call, logMethodReference ) );

                            Instruction last = methodDefinition.Body.Instructions[
                                methodDefinition.Body.Instructions.Count - 1];
                            ilProcessor.InsertBefore( last, Instruction.Create( OpCodes.Ldstr, 
                                "Exit " + typeDefinition.FullName + "." + methodDefinition.Name ) );
                            ilProcessor.InsertBefore( last, Instruction.Create( OpCodes.Call, logMethodReference ) );
                            break;
                        }
                    }

                    // Remove the attribute so it won't be processed again
                    if( logAttribute != null )
                    {
                        methodDefinition.CustomAttributes.Remove( logAttribute );
                    }
                }
            }
        }
    }
}

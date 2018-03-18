using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OSG;
using UnityEditor;
using UnityEngine;
using Mono.Reflection;

public class GameEventUsageDescription
{
    public readonly List<string> unusedEvents;
    public readonly GameEventUsers listeners;
    public readonly GameEventUsers invokers;
    public readonly GameEventUsers[] allusers;


    public class GameEventUser
    {
        public readonly string eventName;
        public readonly Type usingType;
        public readonly MethodInfo addingMethod;
        public GameEventUser(MethodInfo addingMethod, Type usingType, string eventName)
        {
            this.addingMethod = addingMethod;
            this.usingType = usingType;
            this.eventName = eventName;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder("Events:\n");
        unusedEvents.Aggregate(sb, (s, e) => s.AppendFormat(" {0}", e));
        sb.Append("\n");
        listeners.ToString(sb);
        invokers.ToString(sb);
        return sb.ToString();
    }

    public class GameEventUsers : IEnumerable
    {
        public readonly string usedMethod;
        public readonly List<GameEventUser> users;
        public readonly string displayName;
        public readonly GameEventUsageDescription owner;

        public void ToString(StringBuilder sb)
        {
            users.Aggregate(sb, (s,u) => s.AppendFormat("{0}.{1} {2} {3}\n", u.usingType.Name,
                u.addingMethod.Name, displayName, u.eventName));
        }

        public GameEventUsers(string usedMethod, string displayName, GameEventUsageDescription owner)
        {
            this.usedMethod = usedMethod;
            this.displayName = displayName;
            this.owner = owner;
            users = new List<GameEventUser>();
        }

        public bool TryAddCall(Type callerType, MethodInfo callingMethod, MethodInfo calledMethod, Instruction instruction)
        {
            if(calledMethod.Name != usedMethod)
                return false;
            if(!IsAGameEventMethod(calledMethod))
                return false;

            for (var prev = instruction.Previous; prev != null; prev = prev.Previous)
            {
                if (prev.Operand == null)
                    continue;
                FieldInfo info = prev.Operand as FieldInfo;
                if (info == null)
                    continue;
                if (info.DeclaringType != typeof(EventHolder))
                    continue;
                owner.unusedEvents.Remove(info.Name);
                users.Add(new GameEventUser(callingMethod, callerType, info.Name));
                break;
            }

            return true;
        }

        public IEnumerator GetEnumerator()
        {
            return users.GetEnumerator();
        }
    }
    
    [MenuItem("Test/Get GameEvent calls")]
    public static void GetGameEventCalls()
    {
        Debug.Log(new GameEventUsageDescription().ToString());
    }

    public GameEventUsageDescription()
    {
        listeners = new GameEventUsers("AddListener", "Listens", this);
        invokers = new GameEventUsers("Invoke", "Triggers", this);
        allusers = new []{listeners,invokers};

        unusedEvents = GetAllEventNames().ToList();
        LookForMethodCallers();
    }

    public IEnumerable<string> GetAllEventNames()
    {
        return GetAllEventFieldInfos().Select(i=>i.Name);
    }

    private FieldInfo[] GetAllEventFieldInfos()
    {
        return typeof(EventHolder).GetFields(BindingFlags.Public|BindingFlags.Instance);
    }

    private static  bool IsAGameEventMethod(MethodInfo mI)
    {
        return mI.DeclaringType.DerivesFrom(mI.DeclaringType.IsGenericType ? typeof(GameEvent<>) : typeof(GameEvent));
    }
  
    private static IEnumerable<MethodInfo> GetAllMethods(IEnumerable<Type> eventTypes)
    {
        return eventTypes.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public));
    }

    private void LookForMethodCallers()
    {
        AssemblyScanner.Register(type => {
            var methods = type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);

            foreach (MethodInfo methodInfo in methods)
            {
                if(methodInfo.IsAbstract)
                    continue;
                MethodBody methodBody = methodInfo.GetMethodBody();
                if(methodBody==null)
                    continue;
                try{
                List<Instruction> instructions = MethodBodyReader.GetInstructions(methodInfo);
                foreach (var instruction in instructions)
                {
                    MethodInfo calledMethod = instruction.Operand as MethodInfo;
                    if(calledMethod==null) 
                        continue;
                    foreach (var users in allusers)
                    {
                        if(users.TryAddCall(type, methodInfo, calledMethod, instruction))
                            break;
                    }
                }
                }catch(Exception e)
                {
                    //Debug.LogError("On " + methodInfo.DeclaringType.Name+"."+methodInfo.Name);
                    //Debug.LogException(e);
                }
            }},
            AssemblyScanner.OnlyProject
        );
        AssemblyScanner.Scan();  
    }
}

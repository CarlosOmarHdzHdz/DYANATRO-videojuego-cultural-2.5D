using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Animador de combate del protagonista
// ACCION: conservar los clips existentes y conectar mano/Macuahuitl con triggers.
// MODIFICACION: se puede ejecutar manualmente sin reconstruir el resto del nivel.
// ============================================================================
public static class XunjuuPlayerCombatAnimatorBuilder
{
    private const string ControllerPath = "Assets/Animation/Player1.controller";
    private const string BasicAttackClipPath = "Assets/Animation/Attack.anim";
    private const string SwordAttackClipPath = "Assets/Sprites/SwordAttack.anim";

    [MenuItem("Xunjuu v0.1/Combate/Reparar animaciones de Player1")]
    public static void RepairPlayerCombatAnimator()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        AnimationClip basicAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BasicAttackClipPath);
        AnimationClip swordAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SwordAttackClipPath);
        if (controller == null || basicAttackClip == null || swordAttackClip == null || controller.layers.Length == 0)
        {
            Debug.LogWarning("Xunjuu v0.1: no se pudo reparar Player1.controller porque faltan sus clips.");
            return;
        }

        EnsureTrigger(controller, "Attack");
        EnsureTrigger(controller, "SwordAttack");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = FindOrCreateState(stateMachine, "Idle", null);
        AnimatorState attackState = FindOrCreateState(stateMachine, "Attack", basicAttackClip);
        AnimatorState swordState = FindOrCreateState(stateMachine, "SwordAttack", swordAttackClip);

        RemoveLegacyAttackTransitions(stateMachine, attackState, swordState);
        ConfigureAttackTransition(stateMachine, attackState, "Attack");
        ConfigureAttackTransition(stateMachine, swordState, "SwordAttack");
        ConfigureReturnTransition(attackState, idleState);
        ConfigureReturnTransition(swordState, idleState);

        attackState.tag = "XunjuuBasicAttack";
        swordState.tag = "XunjuuSwordAttack";

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[XUNJUU COMBATE PASS] Player1 conserva Attack y SwordAttack con duracion y triggers correctos.");
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
    {
        AnimatorState state = stateMachine.states.Select(child => child.state).FirstOrDefault(candidate => candidate.name == stateName);
        if (state == null)
            state = stateMachine.AddState(stateName);
        if (motion != null)
            state.motion = motion;
        state.speed = 1f;
        return state;
    }

    private static void EnsureTrigger(AnimatorController controller, string parameterName)
    {
        AnimatorControllerParameter existing = controller.parameters.FirstOrDefault(parameter => parameter.name == parameterName);
        if (existing != null && existing.type == AnimatorControllerParameterType.Trigger)
            return;

        if (existing != null)
            controller.RemoveParameter(existing);
        controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
    }

    // ========================================================================
    // Xunjuu v0.1 - Limpieza de rutas de ataque heredadas
    // ACCION: impedir que SwordAttack desemboque en Attack al terminar el tajo.
    // ========================================================================
    private static void RemoveLegacyAttackTransitions(
        AnimatorStateMachine stateMachine,
        AnimatorState attackState,
        AnimatorState swordState)
    {
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            if (transition.destinationState == attackState || transition.destinationState == swordState)
                stateMachine.RemoveAnyStateTransition(transition);
        }

        foreach (ChildAnimatorState child in stateMachine.states)
        {
            foreach (AnimatorStateTransition transition in child.state.transitions.ToArray())
            {
                if (transition.destinationState == attackState || transition.destinationState == swordState)
                    child.state.RemoveTransition(transition);
            }
        }

        foreach (AnimatorTransition transition in stateMachine.entryTransitions.ToArray())
        {
            if (transition.destinationState == attackState || transition.destinationState == swordState)
                stateMachine.RemoveEntryTransition(transition);
        }
    }

    private static void ConfigureAttackTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.02f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.orderedInterruption = true;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void ConfigureReturnTransition(AnimatorState state, AnimatorState idleState)
    {
        foreach (AnimatorStateTransition transition in state.transitions.ToArray())
            state.RemoveTransition(transition);

        AnimatorStateTransition returnTransition = state.AddTransition(idleState);
        returnTransition.hasExitTime = true;
        returnTransition.exitTime = 1f;
        returnTransition.duration = 0.03f;
        returnTransition.hasFixedDuration = true;
    }
}

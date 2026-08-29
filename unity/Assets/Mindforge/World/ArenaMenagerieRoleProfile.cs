using System;
using System.Reflection;
using UnityEngine;
using Mindforge.Enemies;
using Mindforge.Journey;

namespace Mindforge.World
{
    /// <summary>
    /// Serialized authoring profile for one Menagerie role. JourneyEnemyController applies
    /// its base archetype defaults in OnEnable; the wave scheduler snapshots the authored
    /// variant before first deactivation, then calls Apply immediately after activation and
    /// before Arm(). This component does not select attacks, advance timers, move bodies or
    /// resolve damage. It only preserves serialized configuration across Unity lifecycle.
    /// </summary>
    public sealed class ArenaMenagerieRoleProfile : MonoBehaviour
    {
        [SerializeField] private JourneyEnemyController enemy;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float desiredDistance = 2f;
        [SerializeField] private float retreatDistance = 1f;
        [SerializeField] private float strafeStrength = 0.2f;
        [SerializeField] private float meleeVerticalReach = 1.45f;
        [SerializeField] private int firstAttackDelayTicks = 78;
        [SerializeField] private EnemyAttackDefinition[] attackDefinitions = Array.Empty<EnemyAttackDefinition>();
        [SerializeField] private bool captured;

        public bool Captured => captured;

        public void ConfigureRuntime(
            JourneyEnemyController controller,
            float authoredMoveSpeed,
            float authoredDesiredDistance,
            float authoredRetreatDistance,
            float authoredStrafeStrength,
            float authoredVerticalReach,
            int authoredFirstAttackDelayTicks,
            EnemyAttackDefinition[] authoredAttacks)
        {
            enemy = controller;
            moveSpeed = authoredMoveSpeed;
            desiredDistance = authoredDesiredDistance;
            retreatDistance = authoredRetreatDistance;
            strafeStrength = authoredStrafeStrength;
            meleeVerticalReach = authoredVerticalReach;
            firstAttackDelayTicks = authoredFirstAttackDelayTicks;
            attackDefinitions = authoredAttacks ?? Array.Empty<EnemyAttackDefinition>();
            captured = true;
            Apply();
        }

        /// <summary>
        /// Snapshot the already-authored JourneyEnemyController values before the scheduler
        /// disables the GameObject for the first time. This happens while the editor-authored
        /// variant is still intact and prevents OnEnable's base-archetype initialization from
        /// erasing the role on later waves.
        /// </summary>
        public void CaptureFromCurrent(JourneyEnemyController controller = null)
        {
            if (controller != null) enemy = controller;
            if (enemy == null) enemy = GetComponent<JourneyEnemyController>();
            if (enemy == null) return;

            moveSpeed = GetField<float>(enemy, "moveSpeed");
            desiredDistance = GetField<float>(enemy, "desiredDistance");
            retreatDistance = GetField<float>(enemy, "retreatDistance");
            strafeStrength = GetField<float>(enemy, "strafeStrength");
            meleeVerticalReach = GetField<float>(enemy, "meleeVerticalReach");
            firstAttackDelayTicks = GetField<int>(enemy, "firstAttackDelayTicks");
            EnemyAttackDefinition[] authored = GetField<EnemyAttackDefinition[]>(enemy, "attackDefinitions");
            attackDefinitions = authored != null ? (EnemyAttackDefinition[])authored.Clone() : Array.Empty<EnemyAttackDefinition>();
            captured = true;
        }

        public void Apply()
        {
            if (!captured) return;
            if (enemy == null) enemy = GetComponent<JourneyEnemyController>();
            if (enemy == null) return;

            SetField(enemy, "moveSpeed", moveSpeed);
            SetField(enemy, "desiredDistance", desiredDistance);
            SetField(enemy, "retreatDistance", retreatDistance);
            SetField(enemy, "strafeStrength", strafeStrength);
            SetField(enemy, "meleeVerticalReach", meleeVerticalReach);
            SetField(enemy, "firstAttackDelayTicks", Mathf.Max(1, firstAttackDelayTicks));
            SetField(enemy, "attackDefinitions", attackDefinitions ?? Array.Empty<EnemyAttackDefinition>());

            MethodInfo rebuild = typeof(JourneyEnemyController).GetMethod(
                "RebuildCooldownState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (rebuild == null)
                throw new MissingMethodException(typeof(JourneyEnemyController).FullName, "RebuildCooldownState");
            rebuild.Invoke(enemy, null);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, fieldName);
            object value = field.GetValue(target);
            return value is T typed ? typed : default;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, fieldName);
            field.SetValue(target, value);
        }
    }
}

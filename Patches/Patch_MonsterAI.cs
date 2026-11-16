using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CUnitMonster))]
    public static class Patch_MonsterAI
    {
        private static FieldInfo _fiPfDir;
        private static FieldInfo _fiSpeed;
        private static FieldInfo _fiForces;
        private static FieldInfo _fiTarget;
        private static FieldInfo _fiTimeAttack;
        private static FieldInfo _fiOnGround;
        private static FieldInfo _fiLastFireTime;
        private static FieldInfo _fiLookingDir;
        private static bool _isInitialized = false;

        private const float HOVER_GRAVITY_COMPENSATION = 35.0f;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void Prefix_Update(CUnitMonster __instance)
        {
            if (!_isInitialized) InitializeReflection();
            if (__instance == null || !__instance.IsAlive()) return;

            string codeName = __instance.UDesc != null ? __instance.UDesc.m_codeName : null;
            if (string.IsNullOrEmpty(codeName) || !MonsterFactory.CustomMonsters.ContainsKey(codeName)) return;

            try
            {
                var monsterData = MonsterFactory.CustomMonsters[codeName];
                ProcessTargeting(__instance);

                CUnit target = _fiTarget.GetValue(__instance) as CUnit;
                if (target == null || !target.IsAlive()) return;

                // 同步怪物面部朝向目标
                UpdateLookingDirection(__instance, target);

                switch (monsterData.AIType)
                {
                    case MonsterAIType.Ground:
                        UpdateGroundMeleeAI(__instance, target);
                        break;
                    case MonsterAIType.Flying:
                        UpdateFlyingMeleeAI(__instance, target);
                        break;
                    case MonsterAIType.GroundRanged:
                        UpdateGroundRangedAI(__instance, target, monsterData);
                        break;
                    case MonsterAIType.FlyingRanged:
                        UpdateFlyingRangedAI(__instance, target, monsterData);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Mod] 怪物 AI 异常: " + ex.Message);
            }
        }

        private static void UpdateLookingDirection(CUnitMonster monster, CUnit target)
        {
            if (_fiLookingDir == null) return;
            Vector2 dir = (target.PosCenter - monster.PosCenter).normalized;
            _fiLookingDir.SetValue(monster, dir);
        }

        // --- 地面远程 AI ---
        private static void UpdateGroundRangedAI(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            float dist = Vector2.Distance(monster.PosCenter, target.PosCenter);
            float range = data.Desc.m_attackDesc.m_range;

            if (dist <= range)
            {
                // 进入射程：阻断原版寻路继续无脑冲脸
                if (_fiPfDir != null)
                {
                    // 若玩家靠太近（小于 4 格），轻微后撤拉扯距离；否则原地定点射击
                    if (dist < 4.0f)
                    {
                        float retreatX = monster.PosCenter.x > target.PosCenter.x ? 0.8f : -0.8f;
                        _fiPfDir.SetValue(monster, new Vector2(retreatX, 0f));
                    }
                    else
                    {
                        _fiPfDir.SetValue(monster, Vector2.zero);
                    }
                }

                // 减速刹车
                Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
                vel.x *= 0.75f;
                _fiSpeed.SetValue(monster, vel);

                FireRangedAttack(monster, target, data);
            }
            else
            {
                // 超出射程：恢复原版地面正常寻路与跳跃
                UpdateGroundMovement(monster);
            }
        }

        // --- 飞行近战 AI ---
        private static void UpdateFlyingMeleeAI(CUnitMonster monster, CUnit target)
        {
            ApplySmoothFlyingMovement(monster, target.PosCenter, 0.96f);
        }

        // --- 飞行远程 AI ---
        private static void UpdateFlyingRangedAI(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            float dist = Vector2.Distance(monster.PosCenter, target.PosCenter);
            float range = data.Desc.m_attackDesc.m_range;

            if (dist <= range)
            {
                // 在射程内悬停并开火
                Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
                vel *= 0.90f; // 稳定悬停阻尼

                // 轻度悬浮在玩家斜上方 3 格位置，避免贴脸
                Vector2 hoverTarget = target.PosCenter + new Vector2(monster.PosCenter.x > target.PosCenter.x ? 6f : -6f, 3f);
                Vector2 driftDir = (hoverTarget - monster.PosCenter).normalized;
                vel += driftDir * (5f * Time.deltaTime);

                _fiSpeed.SetValue(monster, vel);

                FireRangedAttack(monster, target, data);
            }
            else
            {
                // 追逐玩家
                ApplySmoothFlyingMovement(monster, target.PosCenter + new Vector2(0f, 2f), 0.97f);
            }
        }

        // --- 平滑飞行运动核心逻辑 ---
        private static void ApplySmoothFlyingMovement(CUnitMonster monster, Vector2 targetPos, float damping)
        {
            Vector2 pos = monster.PosCenter;
            Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
            Vector2 dir = (targetPos - pos).normalized;
            float maxSpeed = monster.UDesc != null ? monster.UDesc.GetSpeedMax() : 6.0f;
            float accel = 25.0f;
            float dt = Time.deltaTime;

            // 直接通过速度矢量控制全向飞行，克服地面网格寻路局限
            vel += dir * (accel * dt);
            if (vel.magnitude > maxSpeed)
            {
                vel = vel.normalized * maxSpeed;
            }

            // 空气阻尼保持飞行手感
            vel *= damping;
            _fiSpeed.SetValue(monster, vel);

            // 抵消原版重力持续下拉
            Vector2 forces = (Vector2)_fiForces.GetValue(monster);
            forces.y += HOVER_GRAVITY_COMPENSATION * dt;
            _fiForces.SetValue(monster, forces);

            // 清理地面寻路方向干扰
            if (_fiPfDir != null)
            {
                _fiPfDir.SetValue(monster, dir);
            }
        }

        private static void UpdateGroundMovement(CUnitMonster monster)
        {
            Vector2 pfDir = (Vector2)_fiPfDir.GetValue(monster);
            Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
            Vector2 forces = (Vector2)_fiForces.GetValue(monster);
            bool onGround = (bool)_fiOnGround.GetValue(monster);
            float maxSpeed = monster.UDesc.GetSpeedMax();
            float accel = 40.0f;
            float dt = Time.deltaTime;

            if (pfDir.x > 0.1f && vel.x < maxSpeed) forces.x += accel * dt;
            else if (pfDir.x < -0.1f && vel.x > -maxSpeed) forces.x -= accel * dt;

            bool isStuck = Math.Abs(pfDir.x) > 0.5f && Math.Abs(vel.x) < 0.1f;
            if (onGround && (pfDir.y > 0.1f || isStuck))
            {
                forces.y += 22.0f;
                forces.x += Math.Sign(pfDir.x) * 5.0f;
            }
            _fiForces.SetValue(monster, forces);
        }

        private static void FireRangedAttack(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            float lastFireTime = (float)_fiLastFireTime.GetValue(monster);
            float cooldown = data.Desc.m_attackDesc.m_cooldown;
            float speedMult = StimEnvironmentManager.GetRangedAttackSpeedMultiplier();
            if (speedMult > 1f) cooldown /= speedMult;

            if (GVars.SimuTime < lastFireTime + cooldown) return;

            SBullets sBullets = GameUtils.GetSBullets();
            if (sBullets == null || data.BulletDesc == null) return;

            CAttackDesc baseAttack = data.Desc.m_attackDesc;
            CAttackDesc freshAttack = new CAttackDesc(
                baseAttack.m_range,
                baseAttack.GetDamage(monster),
                1,
                cooldown,
                0f,
                5f,
                data.BulletDesc,
                "plasma"
            );
            Traverse.Create(freshAttack).Field("m_isFromMonster").SetValue(true);

            // 发射子弹：以怪物中心为起点，精准指向目标中心
            Vector2 firePos = monster.PosCenter;
            sBullets.FireBullet(freshAttack, monster, firePos, target.PosCenter);

            _fiLastFireTime.SetValue(monster, GVars.SimuTime);
        }

        private static void ProcessTargeting(CUnitMonster monster)
        {
            object targetObj = _fiTarget.GetValue(monster);
            CUnit targetUnit = targetObj as CUnit;
            if (targetUnit == null || !targetUnit.IsAlive())
            {
                targetUnit = SUnits.GetClosestPlayer(monster.Pos, 100f);
                if (targetUnit != null)
                {
                    _fiTarget.SetValue(monster, targetUnit);
                }
            }
        }

        private static void InitializeReflection()
        {
            try
            {
                _fiTarget = AccessTools.Field(typeof(CUnitMonster), "m_target");
                _fiTimeAttack = AccessTools.Field(typeof(CUnitMonster), "m_timeAttack");
                _fiLastFireTime = AccessTools.Field(typeof(CUnit), "m_lastFireTime");
                _fiPfDir = AccessTools.Field(typeof(CUnitPF), "m_pfDir");
                if (_fiPfDir == null) _fiPfDir = AccessTools.Field(typeof(CUnitMonster), "m_pfDir");
                _fiSpeed = AccessTools.Field(typeof(CUnit), "m_speed");
                _fiForces = AccessTools.Field(typeof(CUnit), "m_forces");
                _fiOnGround = AccessTools.Field(typeof(CUnit), "m_onGround");
                _fiLookingDir = AccessTools.Field(typeof(CUnit), "m_lookingDirection");
            }
            finally
            {
                _isInitialized = true;
            }
        }
    }
}

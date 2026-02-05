using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MyFirstMod
{
    [HarmonyPatch(typeof(CUnitMonster))]
    public static class Patch_MonsterAI
    {
        // 字段缓存
        private static FieldInfo _fiPfDir, _fiSpeed, _fiForces, _fiTarget, _fiTimeAttack, _fiOnGround, _fiLastFireTime;
        private static bool _isInitialized = false;
        
        private const float GRAVITY_COMPENSATION = 50.0f;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void Prefix_Update(CUnitMonster __instance)
        {
            if (!_isInitialized) InitializeReflection();
            if (!__instance.IsAlive()) return;

            string codeName = __instance.UDesc.m_codeName;
            if (!MonsterFactory.CustomMonsters.ContainsKey(codeName)) return;

            try
            {
                var monsterData = MonsterFactory.CustomMonsters[codeName];
                ProcessTargeting(__instance);

                object targetObj = _fiTarget.GetValue(__instance);
                if (targetObj == null) return;

                switch (monsterData.AIType)
                {
                    case MonsterAIType.Ground:       UpdateGroundMeleeAI(__instance, (CUnit)targetObj); break;
                    case MonsterAIType.Flying:       UpdateFlyingAI(__instance, (CUnit)targetObj);      break;
                    case MonsterAIType.GroundRanged: UpdateGroundRangedAI(__instance, (CUnit)targetObj, monsterData); break;
                    case MonsterAIType.FlyingRanged: UpdateFlyingRangedAI(__instance, (CUnit)targetObj, monsterData); break;
                }
            }
            catch { }
        }
        
        // --- 远程AI逻辑 (地面) ---
        private static void UpdateGroundRangedAI(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            if (IsInAttackRange(monster, target, data.Desc.m_attackDesc.m_range))
            {
                BrakeAndFire(monster, target, data);
            }
            else
            {
                UpdateGroundMovement(monster);
            }
        }

        // --- 远程AI逻辑 (飞行) ---
        private static void UpdateFlyingRangedAI(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            Vector2 forces = (Vector2)_fiForces.GetValue(monster);
            forces.y += GRAVITY_COMPENSATION * Time.deltaTime;

            if (IsInAttackRange(monster, target, data.Desc.m_attackDesc.m_range))
            {
                HoverAndFire(monster, target, data);
            }
            else 
            {
                UpdateFlyingMovement(monster, target, forces);
            }
            
            _fiForces.SetValue(monster, forces);
        }

        // --- 行为封装 ---
        private static void BrakeAndFire(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
            vel.x *= 0.8f;
            _fiSpeed.SetValue(monster, vel);
            FireRangedAttack(monster, target, data);
        }
        
        private static void HoverAndFire(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
            vel *= 0.95f;
            _fiSpeed.SetValue(monster, vel);
            FireRangedAttack(monster, target, data);
        }
        
        private static bool IsInAttackRange(CUnitMonster monster, CUnit target, float range)
        {
            return Vector2.Distance(monster.PosCenter, target.PosCenter) < range;
        }

        // --- 【核心修复】每次开火都创建全新的 CAttackDesc ---
        private static void FireRangedAttack(CUnitMonster monster, CUnit target, MonsterFactory.CustomMonsterData data)
        {
            float lastFireTime = (float)_fiLastFireTime.GetValue(monster);
            if (Time.time < lastFireTime + data.Desc.m_attackDesc.m_cooldown) return;

            SBullets sBullets = GameUtils.GetSBullets();
            if (sBullets == null || data.BulletDesc == null) return;
            
            // 1. 获取基础攻击描述
            CAttackDesc baseAttack = data.Desc.m_attackDesc;
            
            // 2. 每次开火都 new 一个全新的 CAttackDesc 对象
            //    这完全模仿了游戏原版玩家开火时的行为，确保没有任何状态被共享
            CAttackDesc freshAttack = new CAttackDesc(
                baseAttack.m_range, 
                baseAttack.GetDamage(monster), 
                1, // nbAttacks
                baseAttack.m_cooldown,
                0f, // knockbackOwn
                5f, // knockbackTarget
                data.BulletDesc, // projDesc
                "plasma" // sound
            );

            // 3. 使用这个全新的对象来开火
            sBullets.FireBullet(freshAttack, monster, monster.PosFire, target.PosCenter);
            
            // 4. 更新CD
            _fiLastFireTime.SetValue(monster, Time.time);
        }
        
        // --- 通用 AI 方法 (保持不变) ---
        private static void ProcessTargeting(CUnitMonster monster)
        {
            object targetObj = _fiTarget.GetValue(monster);
            CUnit targetUnit = targetObj as CUnit;
            if (targetUnit == null || !targetUnit.IsAlive())
            {
                targetUnit = SUnits.GetClosestPlayer(monster.Pos, 100f);
                if (targetUnit != null) _fiTarget.SetValue(monster, targetUnit);
            }
        }
        
        private static void UpdateGroundMeleeAI(CUnitMonster monster, CUnit target)
        {
            float dist = Math.Abs(monster.Pos.x - target.Pos.x);
            float timeAttack = (_fiTimeAttack != null) ? (float)_fiTimeAttack.GetValue(monster) : 0f;
            if (timeAttack > 0 || dist < 1.2f)
            {
                Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
                vel.x *= 0.8f;
                _fiSpeed.SetValue(monster, vel);
                return;
            }
            UpdateGroundMovement(monster);
        }

        private static void UpdateFlyingAI(CUnitMonster monster, CUnit target)
        {
            Vector2 forces = (Vector2)_fiForces.GetValue(monster);
            forces.y += GRAVITY_COMPENSATION * Time.deltaTime;
            _fiForces.SetValue(monster, forces);
            UpdateFlyingMovement(monster, target, forces);
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
        
        private static void UpdateFlyingMovement(CUnitMonster monster, CUnit target, Vector2 forces)
        {
            Vector2 vel = (Vector2)_fiSpeed.GetValue(monster);
            Vector2 targetPos = target.PosCenter;
            targetPos.y += 1.0f; 
            Vector2 dirToTarget = (targetPos - monster.PosCenter).normalized;
            float maxSpeed = monster.UDesc.GetSpeedMax();
            float accel = 20.0f;
            float dt = Time.deltaTime;
            if (dirToTarget.x > 0.1f && vel.x < maxSpeed) forces.x += accel * dt;
            else if (dirToTarget.x < -0.1f && vel.x > -maxSpeed) forces.x -= accel * dt;
            if (dirToTarget.y > 0.1f && vel.y < maxSpeed) forces.y += accel * dt;
            else if (dirToTarget.y < -0.1f && vel.y > -maxSpeed) forces.y -= accel * dt;
            vel *= 0.98f;
            _fiSpeed.SetValue(monster, vel);
            _fiForces.SetValue(monster, forces);
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
            }
            finally { _isInitialized = true; }
        }
    }
}
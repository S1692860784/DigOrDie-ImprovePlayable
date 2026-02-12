using System;
using System.Collections.Generic;

namespace MyFirstMod
{
    public static class StimEnvironmentManager
    {
        public const string StimEventId = "stimBoost";
        private static CEnvironment _stimEnv;
        private static CEnvironment _lastEnv;
        private static float _moveSpeedMult = 1.5f;
        private static float _forceEndTime = -1f;
        private static readonly System.Random _rng = new System.Random();

        public static void RegisterStimEvent()
        {
            List<CEnvironment> envs = SDataLua.GetDescList<CEnvironment>("list_environments");
            if (envs == null) return;

            foreach (var env in envs)
            {
                if (env != null && env.m_id == StimEventId)
                {
                    _stimEnv = env;
                    return;
                }
            }

            _stimEnv = new CEnvironment();
            _stimEnv.m_id = StimEventId;
            _stimEnv.m_duration = 2 * 60f;
            _stimEnv.m_minDay = 2;
            _stimEnv.m_monsterSpeedMult = 1.5f;

            envs.Add(_stimEnv);
        }

        public static void ForceStimEvent(float durationSeconds)
        {
            if (_stimEnv == null) RegisterStimEvent();
            _forceEndTime = GVars.SimuTime + durationSeconds;
            _lastEnv = null;
        }

        public static bool IsStimActive()
        {
            CEnvironment env = GetActiveEnvironment();
            if (env == null) return false;
            if (env == _stimEnv || env.m_id == StimEventId)
            {
                EnsureMoveMultiplier(env);
                return true;
            }
            return false;
        }

        public static float GetMoveSpeedMultiplier()
        {
            CEnvironment env = GetActiveEnvironment();
            if (env == null) return 1f;
            if (!(env == _stimEnv || env.m_id == StimEventId)) return 1f;
            EnsureMoveMultiplier(env);
            return _moveSpeedMult;
        }

        private static void EnsureMoveMultiplier(CEnvironment env)
        {
            if (_lastEnv != env)
            {
                _moveSpeedMult = 1.5f + (float)_rng.NextDouble() * 0.5f;
                _lastEnv = env;
            }

            if (env != null && env.m_monsterSpeedMult != _moveSpeedMult)
            {
                env.m_monsterSpeedMult = _moveSpeedMult;
            }
        }

        public static float GetRangedAttackSpeedMultiplier()
        {
            return IsStimActive() ? 1.5f : 1f;
        }

        public static bool IsForcedActive()
        {
            return _forceEndTime > 0f && GVars.SimuTime < _forceEndTime;
        }

        public static CEnvironment GetActiveEnvironment()
        {
            if (IsForcedActive())
            {
                return _stimEnv ?? SEnvironment.GetEnvironmentCurrent();
            }
            return SEnvironment.GetEnvironmentCurrent();
        }
    }
}

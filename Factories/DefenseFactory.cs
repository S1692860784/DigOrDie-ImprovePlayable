using HarmonyLib;
using System.Reflection;

namespace ImprovePlayable
{
    public static class DefenseFactory
    {
        public static CItem_Defense CreateAndRegisterDefenseFromTemplate(
            CItem_Defense template,
            string codeName,
            string displayName,
            string description,
            ushort hp,
            float rangeDetection,
            float angleMin,
            float angleMax,
            float attackRange,
            int attackDamage,
            int attackNb,
            float attackCooldown,
            float knockbackOwn,
            float knockbackTarget,
            CBulletDesc bulletDesc,
            string soundName)
        {
            if (template == null) return null;
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            CItem_Defense newDefense = ShallowClone(template);

            CAttackDesc attack;
            if (template.m_attack != null)
            {
                attack = ShallowClone(template.m_attack);
                var attackTrav = Traverse.Create(attack);
                attackTrav.Field("m_range").SetValue(attackRange);
                attackTrav.Field("m_damage").SetValue(attackDamage);
                attackTrav.Field("m_nbAttacks").SetValue(attackNb);
                attackTrav.Field("m_cooldown").SetValue(attackCooldown);
                attackTrav.Field("m_knockbackOwn").SetValue(knockbackOwn);
                attackTrav.Field("m_knockbackTarget").SetValue(knockbackTarget);
                attackTrav.Field("m_bulletDesc").SetValue(bulletDesc);
                attackTrav.Field("m_isFromMonster").SetValue(false);
            }
            else
            {
                attack = new CAttackDesc(
                    attackRange,
                    attackDamage,
                    attackNb,
                    attackCooldown,
                    knockbackOwn,
                    knockbackTarget,
                    bulletDesc,
                    soundName);
            }

            var trav = Traverse.Create(newDefense);
            trav.Field("m_codeName").SetValue(codeName);
            trav.Field("m_locTextId").SetValue(codeName);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);
            trav.Field("m_hpMax").SetValue(hp);
            trav.Field("m_rangeDetection").SetValue(rangeDetection);
            trav.Field("m_angleMinDeg").SetValue(angleMin);
            trav.Field("m_angleMaxDeg").SetValue(angleMax);
            trav.Field("m_attack").SetValue(attack);

            newDefense.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newDefense);
            return newDefense;
        }

        private static T ShallowClone<T>(T source) where T : class
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)method.Invoke(source, null);
        }
    }
}

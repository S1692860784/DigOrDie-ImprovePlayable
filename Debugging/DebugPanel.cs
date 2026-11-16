using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovePlayable
{
    public class DebugPanel : MonoBehaviour
    {
        private bool _isOpen = false;
        private Rect _windowRect = new Rect(40, 40, 520, 460);
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "玩家与世界", "物品给予", "怪物生成", "矿物与方块" };
        private Vector2 _scrollPos = Vector2.zero;

        // 快捷物品列表
        private static readonly string[][] QuickItems = new string[][]
        {
            new string[] { "ThanosGun", "能量步枪 (武器)" },
            new string[] { "ImprovePlayable_EnergyCrystal", "能量水晶 (材料)" },
            new string[] { "ImprovePlayable_EnergyCrystalOre", "能量水晶矿 (方块)" },
            new string[] { "ImprovePlayable_ObsidianWall", "黑曜石墙 (建筑)" },
            new string[] { "ImprovePlayable_EnergyCrystalArmor", "能量水晶护甲 (装备)" },
            new string[] { "ImprovePlayable_Teleporter", "能量传送器 (激活道具)" },
            new string[] { "ImprovePlayable_SuperFlashLight", "能量超亮手电 (照明)" },
            new string[] { "ImprovePlayable_TurretEnergy", "360°能量炮塔 (防御)" },
            new string[] { "ImprovePlayable_UraniumGenerator", "自律铀发电机 5kW (电力)" },
            new string[] { "ImprovePlayable_AutoBuilder", "模组专用制造台 modV1" }
        };

        void Update()
        {
            // 按 F1 切换面板显示/隐藏
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _isOpen = !_isOpen;
            }
        }

        void OnGUI()
        {
            if (!_isOpen) return;

            // 限制窗口最小与最大尺寸，支持全屏幕自适应
            _windowRect = GUI.Window(987654, _windowRect, DrawWindow, "Improve Playable 调试控制面板 [按 F1 显隐]");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // 顶部选项卡切换
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(28));
            GUILayout.Space(8);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            switch (_selectedTab)
            {
                case 0:
                    DrawPlayerAndWorldTab();
                    break;
                case 1:
                    DrawItemsTab();
                    break;
                case 2:
                    DrawMonstersTab();
                    break;
                case 3:
                    DrawOresAndBlocksTab();
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("提示: 窗口标题栏可随意拖动 | 快捷键 F1 开关", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("关闭", GUILayout.Width(80)))
            {
                _isOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // 允许拖动整个窗口
            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DrawPlayerAndWorldTab()
        {
            GUILayout.Label("<b>【玩家状态与控制】</b>");
            if (G.m_player != null)
            {
                Vector2 pos = G.m_player.Pos;
                GUILayout.Label(string.Format("玩家坐标: X={0:0.0}, Y={1:0.0} (深度: {2:0.0}m)", pos.x, pos.y, pos.y));
                GUILayout.Label(string.Format("当前生命值 HP: {0:0}/{1:0}", G.m_player.Hp, G.m_player.UDesc != null ? G.m_player.UDesc.m_hpMax : 100));

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("瞬移到鼠标光标位置", GUILayout.Height(28)))
                {
                    TeleportToMouse();
                }
                if (GUILayout.Button("满血复活 / 治愈", GUILayout.Height(28)))
                {
                    if (G.m_player.UDesc != null)
                    {
                        G.m_player.Hp = G.m_player.UDesc.m_hpMax;
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("未检测到活体玩家（请先进入游戏世界）");
            }

            GUILayout.Space(12);
            GUILayout.Label("<b>【环境与时间控制】</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Time.timeScale > 1.1f ? "关闭加速 (恢复1x)" : "开启时间加速 (10x)", GUILayout.Height(28)))
            {
                Time.timeScale = Time.timeScale > 1.1f ? 1.0f : 10.0f;
            }
            if (GUILayout.Button("触发兴奋剂环境 (120秒)", GUILayout.Height(28)))
            {
                StimEnvironmentManager.ForceStimEvent(120f);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawItemsTab()
        {
            GUILayout.Label("<b>【模组物品与科技一键给予】</b>");
            GUILayout.Label("点击直接在脚下掉落该物品拾取物：");
            GUILayout.Space(4);

            for (int i = 0; i < QuickItems.Length; i++)
            {
                string code = QuickItems[i][0];
                string name = QuickItems[i][1];

                GUILayout.BeginHorizontal();
                GUILayout.Label(name, GUILayout.Width(280));
                if (GUILayout.Button("获得 x1", GUILayout.Width(80)))
                {
                    GiveItem(code, 1);
                }
                if (GUILayout.Button("获得 x20", GUILayout.Width(80)))
                {
                    GiveItem(code, 20);
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawMonstersTab()
        {
            GUILayout.Label("<b>【自定义怪物生态生成】</b>");
            GUILayout.Label("点击在玩家右侧生成指定怪物（包含动态 JSON 加载的怪物）：");
            GUILayout.Space(6);

            if (MonsterFactory.CustomMonsters != null && MonsterFactory.CustomMonsters.Count > 0)
            {
                foreach (var kvp in MonsterFactory.CustomMonsters)
                {
                    string code = kvp.Key;
                    var data = kvp.Value;
                    string displayName = code;
                    if (data != null && data.Desc != null)
                    {
                        displayName = string.Format("{0} [HP:{1}, AI:{2}]", code, data.Desc.m_hpMax, data.AIType);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(displayName, GUILayout.Width(300));
                    if (GUILayout.Button("生成 x1", GUILayout.Width(75)))
                    {
                        SpawnMonster(code, 1);
                    }
                    if (GUILayout.Button("生成 x3", GUILayout.Width(75)))
                    {
                        SpawnMonster(code, 3);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("暂无自定义怪物数据。");
            }

            GUILayout.Space(10);
            if (GUILayout.Button("扫描并将所有游戏怪物 CodeName 输出到日志", GUILayout.Height(26)))
            {
                InputHandlerDumpMonsterIds();
            }
        }

        private void DrawOresAndBlocksTab()
        {
            GUILayout.Label("<b>【矿脉与方块建造测试】</b>");
            GUILayout.Space(4);

            if (GUILayout.Button("在光标所在单元格放置：能量水晶矿方块", GUILayout.Height(30)))
            {
                PlaceBlockAtCursor("ImprovePlayable_EnergyCrystalOre");
            }

            if (GUILayout.Button("在光标所在单元格放置：黑曜石墙体", GUILayout.Height(30)))
            {
                PlaceBlockAtCursor("ImprovePlayable_ObsidianWall");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>【世界生成算法测试】</b>");
            if (GUILayout.Button("立即强行触发一次能量水晶矿脉生成 (深度50~800)", GUILayout.Height(30)))
            {
                int maxY = Mathf.Min(800, SWorld.Gs.y - 10);
                ForceGenerateOre("ImprovePlayable_EnergyCrystalOre", "rock", 50, maxY, 0.05f);
            }
        }

        private void TeleportToMouse()
        {
            if (G.m_player == null) return;
            Vector2 mouseWorld = SCamera.GetPosMouseWorld();
            G.m_player.Pos = mouseWorld;
            G.m_player.Speed = Vector2.zero;
        }

        private void GiveItem(string codeName, int count)
        {
            for (int i = 0; i < count; i++)
            {
                CItem item = ItemFactory.FindItemByCodeName(codeName);
                if (item != null && G.m_player != null)
                {
                    SPickup.Create(item, G.m_player.Pos, Vector2.zero);
                }
            }
        }

        private void SpawnMonster(string codeName, int count)
        {
            if (!MonsterFactory.CustomMonsters.ContainsKey(codeName) || G.m_player == null) return;
            var desc = MonsterFactory.CustomMonsters[codeName].Desc;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = G.m_player.Pos + new Vector2(4f + (i * 1.5f), 1f);
                try
                {
                    SUnits.SpawnUnit(desc, pos, 65535);
                }
                catch (Exception ex)
                {
                    Debug.LogError("生成怪物失败: " + ex.Message);
                }
            }
        }

        private void PlaceBlockAtCursor(string codeName)
        {
            CItem item = ItemFactory.FindItemByCodeName(codeName);
            CItemCell cellItem = item as CItemCell;
            if (cellItem == null) return;

            Vector2 mouseWorld = SCamera.GetPosMouseWorld();
            int2 cellPos = new int2(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(mouseWorld.y));

            if (cellPos.x >= 0 && cellPos.x < SWorld.Gs.x && cellPos.y >= 0 && cellPos.y < SWorld.Gs.y)
            {
                SWorld.Inst.SetCell(cellPos.x, cellPos.y, cellItem);
            }
        }

        private void ForceGenerateOre(string oreCodeName, string replaceCodeName, int minY, int maxY, float chance)
        {
            CItem oreItem = ItemFactory.FindItemByCodeName(oreCodeName);
            CItem replaceItem = ItemFactory.FindItemByCodeName(replaceCodeName);
            if (oreItem == null || replaceItem == null || !(oreItem is CItemCell)) return;

            int count = 0;
            int width = SWorld.Gs.x;
            for (int x = 5; x < width - 5; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    CCell cell = SWorld.Inst.Grid[x, y];
                    if (cell.m_item == replaceItem && UnityEngine.Random.value < chance)
                    {
                        SWorld.Inst.SetCell(x, y, (CItemCell)oreItem);
                        count++;
                    }
                }
            }
            Debug.Log(string.Format("[Mod Debug] 矿脉强制生成完成: 共生成 {0} 块 {1}", count, oreCodeName));
        }

        private void InputHandlerDumpMonsterIds()
        {
            var fields = typeof(GUnits).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            foreach (var field in fields)
            {
                if (typeof(CUnitMonster.CDesc).IsAssignableFrom(field.FieldType))
                {
                    try
                    {
                        var desc = field.GetValue(null) as CUnitMonster.CDesc;
                        if (desc != null) Debug.Log(string.Format("发现怪物: ID='{0}'", desc.m_codeName));
                    }
                    catch { }
                }
            }
        }
    }
}

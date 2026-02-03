using UnityEngine;
using UnityEditor;
using System.IO;
using EggRogue;

/// <summary>
/// 批量创建角色和 Passive 资源的编辑器工具。
/// </summary>
public static class CharacterBatchCreator
{
    private const string PassiveFolderPath = "Assets/EggRogue/Configs/Passives";
    private const string CharacterFolderPath = "Assets/EggRogue/Configs/Characters";
    private const string DatabasePath = "Assets/EggRogue/Configs/CharacterDatabase.asset";

    [MenuItem("EggRogue/批量创建角色和 Passive 资源", false, 100)]
    public static void CreateAllCharactersAndPassives()
    {
        // 确保文件夹存在
        EnsureFolderExists(PassiveFolderPath);
        EnsureFolderExists(CharacterFolderPath);

        // 1. 创建所有 Passive 资源
        var passives = CreateAllPassives();

        // 2. 创建所有角色
        var characters = CreateAllCharacters(passives);

        // 3. 更新 CharacterDatabase
        UpdateCharacterDatabase(characters);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CharacterBatchCreator] 完成！创建了 {passives.Length} 个 Passive 和 {characters.Length} 个角色");
    }

    /// <summary>
    /// 根据 CharacterIcon 文件夹下与角色名同名的图标（如 戈登蛋.png、柯蛋.png）批量创建 20 个蛋蛋职业角色并绑定图标。
    /// </summary>
    [MenuItem("EggRogue/批量创建蛋蛋职业角色（绑定 CharacterIcon）", false, 101)]
    public static void CreateProfessionCharactersWithIcons()
    {
        EnsureFolderExists(CharacterFolderPath);
        var passives = CreateAllPassives();

        CharacterData[] characters = new CharacterData[20];

        // 01 - 戈登蛋：原型戈登·拉姆齐 → 暴躁主厨，可爱版
        characters[0] = CreateProfessionCharacter("Character_戈登蛋", "戈登蛋", c =>
        {
            c.characterName = "戈登蛋";
            c.description = "原型：戈登·拉姆齐。暴躁主厨，但你的蛋是可爱版。稳健型，生命与伤害均衡。";
            SetBaseStats(c, 10f, 2f, 110f, 4.8f, 19f, 9.5f);
            c.passiveAbilities = new[] { passives[6] }; // Tank 钢铁堡垒
        });

        // 02 - 爱因斯蛋：原型爱因斯坦 → 这名字已经是满分答案
        characters[1] = CreateProfessionCharacter("Character_爱因斯蛋", "爱因斯蛋", c =>
        {
            c.characterName = "爱因斯蛋";
            c.description = "原型：爱因斯坦。这名字已经是满分答案。精准远程型，攻击范围与子弹速度提升。";
            SetBaseStats(c, 11f, 1.6f, 85f, 4.5f, 22f, 12f);
            c.passiveAbilities = new[] { passives[8] }; // Sniper
        });

        // 03 - 梵高蛋：原型梵高 → 彩色泼溅 = 灵魂附体
        characters[2] = CreateProfessionCharacter("Character_梵高蛋", "梵高蛋", c =>
        {
            c.characterName = "梵高蛋";
            c.description = "原型：梵高。彩色泼溅 = 灵魂附体。创意爆发型，伤害加成提升。";
            SetBaseStats(c, 10f, 2.2f, 88f, 5.2f, 20f, 10f);
            c.passiveAbilities = new[] { passives[0] }; // DamageMultiplier
        });

        // 04 - 白求蛋：原型白求恩 → 在中文语境里非常正面、专业
        characters[3] = CreateProfessionCharacter("Character_白求蛋", "白求蛋", c =>
        {
            c.characterName = "白求蛋";
            c.description = "原型：白求恩。在中文语境里非常正面、专业。生存型角色，最大生命值提升。";
            SetBaseStats(c, 9f, 1.8f, 100f, 4.5f, 18f, 9f);
            c.passiveAbilities = new[] { passives[4] }; // MaxHealthBoost
        });

        // 05 - 阿姆斯蛋：原型尼尔·阿姆斯特朗 → “蛋的一小步”
        characters[4] = CreateProfessionCharacter("Character_阿姆斯蛋", "阿姆斯蛋", c =>
        {
            c.characterName = "阿姆斯蛋";
            c.description = "原型：尼尔·阿姆斯特朗。“蛋的一小步”。机动型角色，移动速度提升。";
            SetBaseStats(c, 9f, 2f, 82f, 6f, 21f, 10f);
            c.passiveAbilities = new[] { passives[3] }; // MoveSpeedBoost
        });

        // 06 - 福尔摩蛋·蓝（摩蛋）：原型福尔摩斯 → 偏英伦警探风
        characters[5] = CreateProfessionCharacter("Character_摩蛋", "福尔摩蛋", c =>
        {
            c.characterName = "福尔摩蛋·蓝";
            c.description = "原型：福尔摩斯。偏英伦警探风。均衡输出型，伤害加成提升。";
            SetBaseStats(c, 11f, 2f, 95f, 5f, 20f, 10f);
            c.passiveAbilities = new[] { passives[0] }; // DamageMultiplier
        });

        // 07 - 梅林蛋：原型亚瑟王传奇·梅林 → 魔法师里最稳的一个
        characters[6] = CreateProfessionCharacter("Character_梅林蛋", "梅林蛋", c =>
        {
            c.characterName = "梅林蛋";
            c.description = "原型：亚瑟王传奇·梅林。魔法师里最稳的一个。高伤脆皮型，伤害翻倍但生命减半。";
            SetBaseStats(c, 14f, 2.2f, 70f, 5.2f, 24f, 11f);
            c.passiveAbilities = new[] { passives[5] }; // GlassCannon
        });

        // 08 - 袁隆蛋：原型袁隆平 → 非常中国、非常有分量
        characters[7] = CreateProfessionCharacter("Character_袁隆蛋", "笼瓶蛋", c =>
        {
            c.characterName = "袁隆蛋";
            c.description = "原型：袁隆平。非常中国、非常有分量。血牛型角色，最大生命值转化为攻击力。";
            SetBaseStats(c, 7f, 1.8f, 140f, 4.2f, 17f, 9f);
            c.passiveAbilities = new[] { passives[1] }; // HealthToDamage
        });

        // 09 - 贝多蛋：原型贝多芬 → 音乐一响就对了
        characters[8] = CreateProfessionCharacter("Character_贝多蛋", "贝多蛋", c =>
        {
            c.characterName = "贝多蛋";
            c.description = "原型：贝多芬。音乐一响就对了。节奏型角色，射速提升。";
            SetBaseStats(c, 8f, 2.5f, 85f, 5.5f, 20f, 9.5f);
            c.passiveAbilities = new[] { passives[2] }; // FireRateBoost
        });

        // 10 - 柯南蛋：原型柯南·道尔/名侦探柯南 → 商业亲和力极强
        characters[9] = CreateProfessionCharacter("Character_柯南蛋", "柯蛋", c =>
        {
            c.characterName = "柯南蛋";
            c.description = "原型：柯南·道尔 / 名侦探柯南。商业亲和力极强。精准型角色，攻击范围与子弹速度提升。";
            SetBaseStats(c, 10f, 1.5f, 88f, 4.8f, 23f, 12f);
            c.passiveAbilities = new[] { passives[8] }; // Sniper
        });

        // 11 - 火焰蛋：原型现代消防英雄形象 → 消防员英雄 archetype
        characters[10] = CreateProfessionCharacter("Character_火焰蛋", "烈焰蛋", c =>
        {
            c.characterName = "火焰蛋";
            c.description = "原型：现代消防英雄形象。消防员不太适合真人直指，用英雄 archetype。肉盾型，生命翻倍但移速略降。";
            SetBaseStats(c, 10f, 1.6f, 105f, 4.8f, 18f, 8.5f);
            c.passiveAbilities = new[] { passives[6] }; // Tank
        });

        // 12 - 伊丽莎蛋：原型伊丽莎白公主/女王 → 王冠 + 优雅
        characters[11] = CreateProfessionCharacter("Character_伊丽莎蛋", "伊丽莎蛋", c =>
        {
            c.characterName = "伊丽莎蛋";
            c.description = "原型：伊丽莎白公主 / 女王。王冠 + 优雅。优雅生存型，最大生命值提升。";
            SetBaseStats(c, 9f, 1.9f, 100f, 5f, 19f, 9.5f);
            c.passiveAbilities = new[] { passives[4] }; // MaxHealthBoost
        });

        // 13 - 杰克蛋：原型杰克·斯派洛 → 海盗，一看就知道是他
        characters[12] = CreateProfessionCharacter("Character_杰克蛋", "杰克蛋", c =>
        {
            c.characterName = "杰克蛋";
            c.description = "原型：杰克·斯派洛。海盗蛋，一看就知道是他。近战爆发型，伤害与攻速提升、范围略降。";
            SetBaseStats(c, 12f, 2.4f, 92f, 5.5f, 20f, 9f);
            c.passiveAbilities = new[] { passives[7] }; // Berserker
        });

        // 14 - 乔丹蛋：原型迈克尔·乔丹 → 篮球纹理完美匹配
        characters[13] = CreateProfessionCharacter("Character_乔丹蛋", "乔丹蛋", c =>
        {
            c.characterName = "乔丹蛋";
            c.description = "原型：迈克尔·乔丹。运动员蛋，篮球纹理完美匹配。敏捷型，移速与射速双修。";
            SetBaseStats(c, 9f, 2.6f, 88f, 6.2f, 21f, 9.5f);
            c.passiveAbilities = new[] { passives[3] }; // MoveSpeedBoost
        });

        // 15 - 莫奈蛋：原型莫奈 → 园丁，花园自然色彩，很有文艺气质
        characters[14] = CreateProfessionCharacter("Character_莫奈蛋", "莫奈蛋", c =>
        {
            c.characterName = "莫奈蛋";
            c.description = "原型：莫奈。园丁蛋，花园、自然、色彩，很有文艺气质。生存型，最大生命值提升。";
            SetBaseStats(c, 8f, 1.8f, 115f, 4.5f, 18f, 9.5f);
            c.passiveAbilities = new[] { passives[4] }; // MaxHealthBoost
        });

        // 16 - 莱特蛋：原型莱特兄弟 → 飞行史开端
        characters[15] = CreateProfessionCharacter("Character_莱特蛋", "莱特蛋", c =>
        {
            c.characterName = "莱特蛋";
            c.description = "原型：莱特兄弟。飞行员蛋，飞行史开端。机动型，移速提升；围巾飘扬。";
            SetBaseStats(c, 9f, 2f, 80f, 6.2f, 22f, 10f);
            c.passiveAbilities = new[] { passives[3] }; // MoveSpeedBoost
        });

        // 17 - 达尔文蛋：原型达尔文 → 从蛋开始进化，逻辑闭环
        characters[16] = CreateProfessionCharacter("Character_达尔文蛋", "达尔文蛋", c =>
        {
            c.characterName = "达尔文蛋";
            c.description = "原型：达尔文。恐龙蛋，从蛋开始进化，逻辑闭环。血牛向，生命值转化为攻击力。";
            SetBaseStats(c, 8f, 1.7f, 130f, 4.5f, 19f, 9f);
            c.passiveAbilities = new[] { passives[1] }; // HealthToDamage
        });

        // 18 - 爱因蜂蛋：原型爱因斯坦蜜蜂名言梗 → 偏彩蛋向名字
        characters[17] = CreateProfessionCharacter("Character_爱因蜂蛋", "爱因蜂蛋", c =>
        {
            c.characterName = "爱因蜂蛋";
            c.description = "原型：爱因斯坦（“没有蜜蜂就没有人类”的名言梗）。蜜蜂蛋，偏彩蛋向名字。节奏型，射速提升。";
            SetBaseStats(c, 8f, 2.6f, 82f, 5.8f, 20f, 9.5f);
            c.passiveAbilities = new[] { passives[2] }; // FireRateBoost
        });

        // 19 - 加菲蛋：原型加菲猫 → 超级大众化 IP 气质
        characters[18] = CreateProfessionCharacter("Character_加菲蛋", "加菲蛋", c =>
        {
            c.characterName = "加菲蛋";
            c.description = "原型：加菲猫。猫咪蛋，超级大众化 IP 气质。爆发型，伤害加成提升。";
            SetBaseStats(c, 11f, 2.2f, 90f, 4.8f, 20f, 9.5f);
            c.passiveAbilities = new[] { passives[0] }; // DamageMultiplier
        });

        // 20 - 图灵蛋：原型艾伦·图灵 → 科技感、AI 感都对位
        characters[19] = CreateProfessionCharacter("Character_图灵蛋", "图灵蛋", c =>
        {
            c.characterName = "图灵蛋";
            c.description = "原型：艾伦·图灵。机器人蛋，科技感、AI 感都对位。精准型，攻击范围与子弹速度提升。";
            SetBaseStats(c, 10f, 1.6f, 85f, 4.8f, 24f, 12f);
            c.passiveAbilities = new[] { passives[8] }; // Sniper
        });

        UpdateCharacterDatabase(characters);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CharacterBatchCreator] 蛋蛋职业角色创建完成！20 个角色已按图标文件名绑定 CharacterIcon 文件夹，并已更新 CharacterDatabase。");
    }

    private static void SetBaseStats(CharacterData c, float damage, float fireRate, float maxHealth, float moveSpeed, float bulletSpeed, float attackRange)
    {
        c.baseDamage = damage;
        c.baseFireRate = fireRate;
        c.baseMaxHealth = maxHealth;
        c.baseMoveSpeed = moveSpeed;
        c.baseBulletSpeed = bulletSpeed;
        c.baseAttackRange = attackRange;
    }

    /// <summary>
    /// 创建职业角色并绑定图标。iconFileName 为 CharacterIcon 文件夹下的文件名（不含 .png），
    /// 可与角色名一致或近似（如 福尔摩蛋·蓝 用 福尔摩蛋，柯南蛋 用 柯蛋，火焰蛋 用 烈焰蛋，袁隆蛋 用 笼瓶蛋）。
    /// </summary>
    private static CharacterData CreateProfessionCharacter(string assetName, string iconFileName, System.Action<CharacterData> configure)
    {
        CharacterData character = CreateOrLoadCharacter(assetName, _ => { });
        configure(character);
        character.icon = LoadSpriteByIconName(iconFileName);
        ApplyAndSaveCharacter(character);
        return character;
    }

    /// <summary>
    /// 通过 SerializedObject 强制应用修改并立即保存，确保新建/修改的 ScriptableObject 正确写入磁盘。
    /// </summary>
    private static void ApplyAndSaveCharacter(CharacterData character)
    {
        if (character == null) return;
        SerializedObject so = new SerializedObject(character);
        so.Update();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(character);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 按图标文件名从 CharacterIcon 文件夹加载 Sprite（文件名不含 .png）。
    /// </summary>
    private static Sprite LoadSpriteByIconName(string iconFileName)
    {
        if (string.IsNullOrEmpty(iconFileName))
            return null;

        string path = CharacterIconFolderPath + "/" + iconFileName + ".png";
        string fullPath = Path.Combine(Application.dataPath, "..", path).Replace("\\", "/");

        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogWarning($"[CharacterBatchCreator] 未找到图标: {path}");
            return null;
        }

        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object o in all)
        {
            if (o is Sprite s)
                return s;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
            Debug.LogWarning($"[CharacterBatchCreator] {path} 未以 Sprite 导入，请在 Inspector 中设为 Sprite (2D and UI) 后重新运行。");
        return null;
    }

    private const string CharacterIconFolderPath = "Assets/EggRogue/UI/CharacterIcon";

    private static CharacterPassive[] CreateAllPassives()
    {
        CharacterPassive[] passives = new CharacterPassive[9];

        // 1. 伤害倍率 (已存在则加载)
        passives[0] = CreateOrLoadPassive<DamageMultiplierPassive>("DamageMultiplier_1.5x", p =>
        {
            p.abilityName = "力量强化";
            p.description = "所有伤害加成效果 × 1.5";
            p.multiplier = 1.5f;
        });

        // 2. 生命转攻击
        passives[1] = CreateOrLoadPassive<HealthToDamagePassive>("HealthToDamage_20", p =>
        {
            p.abilityName = "以血换力";
            p.description = "将 20% 最大生命值转化为攻击力";
            p.ratio = 0.2f;
        });

        // 3. 射速提升
        passives[2] = CreateOrLoadPassive<FireRateBoostPassive>("FireRateBoost_1.5x", p =>
        {
            p.abilityName = "疾速射击";
            p.description = "射速提升 50%";
            p.multiplier = 1.5f;
        });

        // 4. 移速提升
        passives[3] = CreateOrLoadPassive<MoveSpeedBoostPassive>("MoveSpeedBoost_1.4x", p =>
        {
            p.abilityName = "疾风步";
            p.description = "移动速度提升 40%";
            p.multiplier = 1.4f;
        });

        // 5. 生命提升
        passives[4] = CreateOrLoadPassive<MaxHealthBoostPassive>("MaxHealthBoost_1.5x", p =>
        {
            p.abilityName = "生命强化";
            p.description = "最大生命值提升 50%";
            p.multiplier = 1.5f;
        });

        // 6. 玻璃大炮
        passives[5] = CreateOrLoadPassive<GlassCannonPassive>("GlassCannon", p =>
        {
            p.abilityName = "玻璃大炮";
            p.description = "伤害 × 2，但最大生命值 × 0.5";
            p.damageMultiplier = 2.0f;
            p.healthMultiplier = 0.5f;
        });

        // 7. 坦克
        passives[6] = CreateOrLoadPassive<TankPassive>("Tank", p =>
        {
            p.abilityName = "钢铁堡垒";
            p.description = "最大生命值 × 2，但移速 × 0.7";
            p.healthMultiplier = 2.0f;
            p.speedMultiplier = 0.7f;
        });

        // 8. 狂战士
        passives[7] = CreateOrLoadPassive<BerserkerPassive>("Berserker", p =>
        {
            p.abilityName = "狂战士之怒";
            p.description = "伤害 × 1.5，攻速 × 1.3，但攻击范围 × 0.6";
            p.damageMultiplier = 1.5f;
            p.fireRateMultiplier = 1.3f;
            p.rangeMultiplier = 0.6f;
        });

        // 9. 狙击手
        passives[8] = CreateOrLoadPassive<SniperPassive>("Sniper", p =>
        {
            p.abilityName = "远程狙击";
            p.description = "攻击范围 × 1.8，子弹速度 × 1.5，但射速 × 0.6";
            p.rangeMultiplier = 1.8f;
            p.bulletSpeedMultiplier = 1.5f;
            p.fireRateMultiplier = 0.6f;
        });

        return passives;
    }

    private static CharacterData[] CreateAllCharacters(CharacterPassive[] passives)
    {
        CharacterData[] characters = new CharacterData[8];

        // 角色 1: 力量之蛋 - 伤害倍率
        characters[0] = CreateOrLoadCharacter("Character_力量之蛋", c =>
        {
            c.characterName = "力量之蛋";
            c.description = "专注于伤害输出的角色，所有伤害加成效果提升 50%";
            c.baseDamage = 12f;
            c.baseFireRate = 2f;
            c.baseMaxHealth = 80f;
            c.baseMoveSpeed = 5f;
            c.baseBulletSpeed = 20f;
            c.baseAttackRange = 10f;
            c.passiveAbilities = new[] { passives[0] };
        });

        // 角色 2: 奶大力 - 生命转攻击
        characters[1] = CreateOrLoadCharacter("Character_奶大力", c =>
        {
            c.characterName = "奶大力";
            c.description = "血牛型角色，将最大生命值转化为攻击力";
            c.baseDamage = 8f;
            c.baseFireRate = 2f;
            c.baseMaxHealth = 150f;
            c.baseMoveSpeed = 4.5f;
            c.baseBulletSpeed = 18f;
            c.baseAttackRange = 10f;
            c.passiveAbilities = new[] { passives[1] };
        });

        // 角色 3: 疾风蛋 - 射速提升
        characters[2] = CreateOrLoadCharacter("Character_疾风蛋", c =>
        {
            c.characterName = "疾风蛋";
            c.description = "快速射击的角色，攻速提升 50%";
            c.baseDamage = 8f;
            c.baseFireRate = 3f;
            c.baseMaxHealth = 90f;
            c.baseMoveSpeed = 5.5f;
            c.baseBulletSpeed = 22f;
            c.baseAttackRange = 9f;
            c.passiveAbilities = new[] { passives[2] };
        });

        // 角色 4: 闪电侠蛋 - 移速提升
        characters[3] = CreateOrLoadCharacter("Character_闪电侠蛋", c =>
        {
            c.characterName = "闪电侠蛋";
            c.description = "极速移动的角色，移速提升 40%";
            c.baseDamage = 9f;
            c.baseFireRate = 2f;
            c.baseMaxHealth = 85f;
            c.baseMoveSpeed = 7f;
            c.baseBulletSpeed = 20f;
            c.baseAttackRange = 10f;
            c.passiveAbilities = new[] { passives[3] };
        });

        // 角色 5: 铁壁蛋 - 生命提升
        characters[4] = CreateOrLoadCharacter("Character_铁壁蛋", c =>
        {
            c.characterName = "铁壁蛋";
            c.description = "高生命值的防御型角色，最大生命值提升 50%";
            c.baseDamage = 8f;
            c.baseFireRate = 1.8f;
            c.baseMaxHealth = 120f;
            c.baseMoveSpeed = 4.5f;
            c.baseBulletSpeed = 18f;
            c.baseAttackRange = 9f;
            c.passiveAbilities = new[] { passives[4] };
        });

        // 角色 6: 玻璃蛋 - 玻璃大炮
        characters[5] = CreateOrLoadCharacter("Character_玻璃蛋", c =>
        {
            c.characterName = "玻璃蛋";
            c.description = "极高伤害但非常脆弱的角色，伤害翻倍但生命减半";
            c.baseDamage = 15f;
            c.baseFireRate = 2.5f;
            c.baseMaxHealth = 60f;
            c.baseMoveSpeed = 5.5f;
            c.baseBulletSpeed = 25f;
            c.baseAttackRange = 11f;
            c.passiveAbilities = new[] { passives[5] };
        });

        // 角色 7: 坦克蛋 - 坦克
        characters[6] = CreateOrLoadCharacter("Character_坦克蛋", c =>
        {
            c.characterName = "坦克蛋";
            c.description = "超级肉盾角色，生命翻倍但移动缓慢";
            c.baseDamage = 10f;
            c.baseFireRate = 1.5f;
            c.baseMaxHealth = 100f;
            c.baseMoveSpeed = 5f;
            c.baseBulletSpeed = 15f;
            c.baseAttackRange = 8f;
            c.passiveAbilities = new[] { passives[6] };
        });

        // 角色 8: 狂战士蛋 - 狂战士
        characters[7] = CreateOrLoadCharacter("Character_狂战士蛋", c =>
        {
            c.characterName = "狂战士蛋";
            c.description = "近战输出型角色，高伤害高攻速但攻击范围小";
            c.baseDamage = 12f;
            c.baseFireRate = 2.5f;
            c.baseMaxHealth = 100f;
            c.baseMoveSpeed = 5.5f;
            c.baseBulletSpeed = 20f;
            c.baseAttackRange = 12f;
            c.passiveAbilities = new[] { passives[7] };
        });

        return characters;
    }

    private static void UpdateCharacterDatabase(CharacterData[] characters)
    {
        CharacterDatabase database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.characters = characters;
        EditorUtility.SetDirty(database);
        Debug.Log($"[CharacterBatchCreator] 已更新 CharacterDatabase，包含 {characters.Length} 个角色");
    }

    private static T CreateOrLoadPassive<T>(string assetName, System.Action<T> configure) where T : CharacterPassive
    {
        string path = $"{PassiveFolderPath}/{assetName}.asset";
        T passive = AssetDatabase.LoadAssetAtPath<T>(path);

        if (passive == null)
        {
            passive = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(passive, path);
            Debug.Log($"[CharacterBatchCreator] 创建 Passive: {assetName}");
        }

        configure(passive);
        EditorUtility.SetDirty(passive);
        return passive;
    }

    private static CharacterData CreateOrLoadCharacter(string assetName, System.Action<CharacterData> configure)
    {
        string path = $"{CharacterFolderPath}/{assetName}.asset";
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);

        if (character == null)
        {
            character = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(character, path);
            Debug.Log($"[CharacterBatchCreator] 创建 Character: {assetName}");
        }

        configure(character);
        EditorUtility.SetDirty(character);
        return character;
    }

    private static void EnsureFolderExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
            Debug.Log($"[CharacterBatchCreator] 创建文件夹: {path}");
        }
    }
}

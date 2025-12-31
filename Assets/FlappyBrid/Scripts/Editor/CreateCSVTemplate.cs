using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// 创建带UTF-8 BOM的CSV模板文件
/// </summary>
public class CreateCSVTemplate
{
    [MenuItem("Tools/FlappyBird/创建CSV模板文件（UTF-8 BOM）")]
    public static void CreateTemplate()
    {
        string templatePath = "Assets/FlappyBrid/Level/LevelDataTemplate.csv";
        string templateContent = @"关卡编号,关卡名称,目标时间,生成率倍数,移动速度倍数,高度偏移,管道通过分数,完成奖励,关卡描述,使用关卡道具设置,道具生成概率,道具X轴偏移,使用关卡怪物设置,怪物生成概率,怪物X轴偏移,怪物Y轴偏移,金币产出下限,金币产出上限,产出控制开始比例,产出控制曲线类型
1,关卡1,15,1,0.5,2,1,1,第一关,是,0.3,0,是,0.2,0,0,10,50,0.7,Smooth
2,关卡2,20,1.2,0.6,3,1,2,第二关,是,0.3,0,是,0.2,0,0,15,60,0.7,Smooth
3,关卡3,25,1.5,0.7,4,1,3,第三关,是,0.4,0,是,0.3,0,0,20,70,0.7,Smooth
4,关卡4,30,1.8,0.8,5,1,4,第四关,是,0.4,0,是,0.3,0,0,25,80,0.7,Smooth
5,关卡5,35,2,0.9,6,1,5,第五关,是,0.5,0,是,0.4,0,0,30,90,0.7,Smooth";

        // 使用UTF-8 with BOM编码写入文件
        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
        byte[] contentBytes = Encoding.UTF8.GetBytes(templateContent);
        byte[] fileBytes = new byte[bom.Length + contentBytes.Length];
        
        System.Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        System.Buffer.BlockCopy(contentBytes, 0, fileBytes, bom.Length, contentBytes.Length);
        
        File.WriteAllBytes(templatePath, fileBytes);
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("成功", $"CSV模板文件已创建：\n{templatePath}\n\n现在可以用Excel打开，中文不会乱码了！", "确定");
    }
    
    [MenuItem("Tools/FlappyBird/创建Excel导入模板（CSV格式）")]
    public static void CreateExcelTemplate()
    {
        string templatePath = "Assets/FlappyBrid/Level/LevelDataTemplate_ForExcel.csv";
        string templateContent = @"关卡编号,关卡名称,目标时间,生成率倍数,移动速度倍数,高度偏移,管道通过分数,完成奖励,关卡描述,使用关卡道具设置,道具生成概率,道具X轴偏移,使用关卡怪物设置,怪物生成概率,怪物X轴偏移,怪物Y轴偏移,金币产出下限,金币产出上限,产出控制开始比例,产出控制曲线类型,背景颜色
1,关卡1,15,1,0.5,2,1,1,第一关,是,0.3,0,是,0.2,0,0,10,50,0.7,Smooth,
2,关卡2,20,1.2,0.6,3,1,2,第二关,是,0.3,0,是,0.2,0,0,15,60,0.7,Smooth,
3,关卡3,25,1.5,0.7,4,1,3,第三关,是,0.4,0,是,0.3,0,0,20,70,0.7,Smooth,
4,关卡4,30,1.8,0.8,5,1,4,第四关,是,0.4,0,是,0.3,0,0,25,80,0.7,Smooth,
5,关卡5,35,2,0.9,6,1,5,第五关,是,0.5,0,是,0.4,0,0,30,90,0.7,Smooth,";

        // 使用UTF-8 with BOM编码写入文件
        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
        byte[] contentBytes = Encoding.UTF8.GetBytes(templateContent);
        byte[] fileBytes = new byte[bom.Length + contentBytes.Length];
        
        System.Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        System.Buffer.BlockCopy(contentBytes, 0, fileBytes, bom.Length, contentBytes.Length);
        
        File.WriteAllBytes(templatePath, fileBytes);
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("成功", 
            $"CSV模板已创建：\n{templatePath}\n\n" +
            "使用步骤：\n" +
            "1. 用Excel打开这个CSV文件\n" +
            "2. 编辑关卡数据\n" +
            "3. 保存为CSV UTF-8格式\n" +
            "4. 在Unity中使用'批量导入关卡数据'工具导入该CSV文件\n\n" +
            "注意：背景颜色需要在SO文件中手动设置，不在CSV中导入", 
            "确定");
    }
}


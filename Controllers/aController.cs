using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace BabyNames.Controllers
{
    public class aController : ApiController
    {
        public dynamic getYear(int year)
        {
            try
            {
                string txtFile = "C:/Users/Administrator/Desktop/babynames/names/yob" + year + ".txt";
                StreamReader srReadFile = new StreamReader(txtFile);
                return srReadFile;
            }
            catch
            {
                return null;
            }
        }
        public List<dynamic> loadData(int start, int end)
        {
            List<dynamic> data = new List<dynamic>(); // 储存txt 转换到 json的数据
            for (int i = start; i <= end; i++) // 此处的i也是当前操作的文件的年份
            {

                JObject totals;

                // 读totals文件
                string totalsJsonFile = "C:/Users/Administrator/Desktop/babynames/babynames-master/babynames-master/extra/totals.json";
                System.IO.StreamReader totalsJsonReader = System.IO.File.OpenText(totalsJsonFile);
                using (JsonTextReader reader = new JsonTextReader(totalsJsonReader))
                {
                    totals = (JObject)JToken.ReadFrom(reader); // 将totals文件转换成JObject类型
                }

                StreamReader srReadFile = getYear(i); // 获取year的txt文件数据;
                if (srReadFile == null)
                {
                    continue;
                }
                // 读取流直至文件末尾结束
                while (!srReadFile.EndOfStream)
                {
                    Dictionary<string, dynamic> dataTemp = new Dictionary<string, dynamic>(); // 储存一个文件的数据
                    string strReadLine = srReadFile.ReadLine(); // 读取每行数据
                    string[] strReadLineList = strReadLine.Split(new Char[] { ',' }); // 将数据按逗号分割放入数组
                    if (strReadLineList == null) // 为空continue
                    {
                        continue;
                    }
                    Dictionary<string, dynamic> names = new Dictionary<string, dynamic>();
                    names.Add("name", strReadLineList[0]);
                    names.Add("gender", strReadLineList[1]);
                    names.Add("value", Convert.ToInt32(strReadLineList[2]));

                    decimal percent = names["value"] / Convert.ToDecimal(totals["" + i + ""][names["gender"]]); // 计算公式
                    decimal percents = percent;
                    decimal.TryParse(percent.ToString("f22"), out percents); // 保留22位小数,并返回decimal类型
                    dataTemp.Add("year", i);
                    dataTemp.Add("totals", totals);
                    dataTemp.Add("names", names);
                    dataTemp.Add("percent", percents);
                    data.Add(dataTemp);
                }
            }
            return data;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">只截取name的数据</param>
        /// <param name="start">开始年份</param>
        /// <param name="end">结束年份</param>
        /// <param name="cutoff"></param>
        /// <param name="min"></param>
        [HttpGet]
        public void WriteJson(Boolean store = false, string name = "", int start = 1880, int end = 1890, double cutoff = 0, double min = 0)
        {
            JObject result = new JObject();
            List<string> dataId = new List<string>();

            if (store) // 使用roster文件读取json
            {
                JArray totals;
                List<dynamic> data = new List<dynamic>(); // 储存json文件里的数据
                string totalsJsonFile = "C:/Users/Administrator/Desktop/babynames/test1/roster/roster.json";
                System.IO.StreamReader totalsJsonReader = System.IO.File.OpenText(totalsJsonFile);
                using (JsonTextReader reader = new JsonTextReader(totalsJsonReader))
                {
                    totals = (JArray)JToken.ReadFrom(reader); // 将totals文件转换成JObject类型
                }
                foreach (string i in totals)
                {
                    dataId.Add(i);
                    // i 为文件的name 根据i去读取文件
                    JObject dataSingleJson; // 保存每个小文件的json
                    string jsonFile = "C:/Users/Administrator/Desktop/babynames/test/" + i + ".json";
                    System.IO.StreamReader jsonFileReader = System.IO.File.OpenText(jsonFile);
                    using (JsonTextReader singleReader = new JsonTextReader(jsonFileReader))
                    {
                        dataSingleJson = (JObject)JToken.ReadFrom(singleReader); // 将totals文件转换成JObject类型
                        result.Add(i, dataSingleJson);
                    }
                }
            }
            else
            {
                List<dynamic> filesTxt = new List<dynamic>();

                filesTxt = loadData(start, end); // 使用txt读取转成json
                if (name != "")
                {
                    filesTxt = filesTxt.Where(p => p["names"]["name"] == name).ToList(); // 只截取name的数据
                }
                JObject data = new JObject();
                string filesList, id, KValues;
                int VValues;
                decimal PValues;
                JObject nullData = new JObject();
                JObject nodePercents, nodeValues;
                int filesCount = filesTxt.Count; // 单独写出来提升循环速度

                for (int i = 0; i < filesCount; i++) // 倒叙提升速度
                {
                    filesList = filesTxt[i]["names"];
                    id = filesList["name"] + "-" + filesList["gender"];
                    if (!dataId.Contains(id)) // 不存在id的时候新增一条数据
                    {
                        KValues = Convert.ToString(filesTxt[i]["year"]);
                        VValues = filesTxt[i]["names"]["value"];
                        PValues = filesTxt[i]["percent"];
                        dataId.Add(id);
                        data.Add("_id", id);
                        data.Add("name", filesList["name"]);
                        data.Add("gender", filesList["gender"]);
                        data.Add("values", new JObject(new JProperty(KValues, new JValue(VValues))));
                        data.Add("percents", new JObject(new JProperty(KValues, new JValue(PValues))));
                        data.Add("normalized", nullData);
                        result.Add(id, data); continue;
                    }
                    // 在把新的数据加进去
                    nodePercents = result[id].SelectToken("percents") as JObject;
                    nodePercents.Add(new JProperty(Convert.ToString(filesTxt[i]["year"]), filesTxt[i]["percent"])); // 在percents下面新增数据
                    nodeValues = result[id].SelectToken("values") as JObject;
                    nodeValues.Add(new JProperty(Convert.ToString(filesTxt[i]["year"]), filesTxt[i]["names"]["value"])); // 同理
                }
            }
            string path = "C:/Users/Administrator/Desktop/babynames/flat"; // 保存的路径文件


            // 获取至少出来了多少年的名字
            if (cutoff != 0)
            {
                int allYearCount = 0;
                JObject tempResult = new JObject(result); // 因为下面要修改result 所有这里将要遍历的数据放在一个临时文件中
                foreach (var i in tempResult)
                {
                    JToken json = i.Value;
                    foreach (JProperty JP in json)
                    {
                        if (JP.Name == "values") // 只查找key值为values
                        {
                            allYearCount = JP.Value.Count(); // 长度即为该名称出现的年份次数
                            if (allYearCount < cutoff) // 小于给定的cutoff移除
                            {
                                result.Remove(i.Key); // 移除不符合条件的数据
                            }
                        }
                    }

                }
            }
            // 获取在一年内至少出现多少次的名字
            if (min != 0)
            {
                JObject minResult = new JObject();
                foreach (var i in result)
                {
                    JToken json = i.Value;
                    foreach (JProperty JP in json)
                    {
                        if (JP.Name == "values") // 只查找key值为values
                        {
                            foreach (int m in JP.Value)
                            {
                                if (m >= min)
                                {
                                    minResult.Add(i.Key, i.Value); break; // 将符合条件的数据新增等minResult中
                                }
                            }
                        }
                    }

                }
                result = minResult; // 替换原来的result文件
            }
            foreach (var i in result) // 遍历最后的结果,写入json文件中
            {
                string singleFileName = "/" + i.Key + ".json";
                JToken json = i.Value;
                FileStr(path, singleFileName, json); // 生成json文件
            }

            // 生成phonemes文件代码


            // 生成roster的代码
            path = path + "/roster";
            string fileName = "/roster.json";// 生成roster文件
            FileStr(path, fileName, dataId);
            string fileName_short = "/roster_short.json"; // 生成roster_short文件,此文件包含姓名即有男又有女
            List<string> roster_short = new List<string>();
            foreach (string i in dataId)
            {
                string SingleName = i.Split('-')[0].ToString();
                string SingleGender = i.Split('-')[1].ToString();
                if (SingleGender == "F")
                {
                    if (dataId.IndexOf(SingleName + "-M") != -1)
                    {
                        roster_short.Add(SingleName + " (F)");
                    }
                    else
                    {
                        roster_short.Add(SingleName);
                    }
                }
                else
                {
                    if (dataId.IndexOf(SingleName + "-F") != -1)
                    {
                        roster_short.Add(SingleName + " (M)");
                    }
                    else
                    {
                        roster_short.Add(SingleName);
                    }
                }
            }
            FileStr(path, fileName_short, roster_short);
        }
        public void FileStr(string path, string fileName, dynamic data)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            using (FileStream fs = new FileStream(path + fileName, FileMode.Create))
            {
                //写入
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (data is JToken) // 如果是string类型则data不需要序列化
                    {
                        sw.WriteLine(data);
                    }
                    else
                    {
                        sw.Write(JsonConvert.SerializeObject(data));
                    }
                }

            }
        }
    }
}

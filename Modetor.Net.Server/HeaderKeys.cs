using System;
using System.Collections.Generic;
using System.Linq;
namespace Modetor.Net.Server
{
    public class HeaderKeys
    {
        /// <summary>
        ///     initialize new instance from content header
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static HeaderKeys From(string content) => new HeaderKeys(content);

        /// <summary>
        ///     Build a json string from Dictionary
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static string GenerateJSON(Dictionary<string, string> dic)
        {
            string res = "{";
            foreach (KeyValuePair<string, string> item in dic)
                res += $"\"{item.Key}\": \"{item.Value}\",";
            return res.Substring(0, res.Length - 1) + "}";
        }





        public HeaderKeys(string content)
        {

            Content = content.Split('\n').Select(l => l.Trim()).ToArray();


            dic = new Dictionary<string, string>();
            try { 
                prepare();
                
            }
            catch(Exception exp) { Server.GetServer().Logger.Log(exp); }

        }

        private void prepare()
        {
            bool isPost = false, emptyLineReached = false;
            foreach (string item in Content)
            {
                string[] kv = null;
                if(item.Equals(string.Empty)) { emptyLineReached = true;continue; }
                 
                if(emptyLineReached)
                {
                    if (!item.Equals(string.Empty) && isPost)
                    {
                        string p = item.Trim(), temp = string.Empty;
                        foreach (char c in p)
                        {
                            if (char.IsSeparator(c) || char.IsLetterOrDigit(c) || char.IsSymbol(c) ||
                                char.IsWhiteSpace(c) || char.IsNumber(c) || char.IsPunctuation(c))
                                temp += c;
                        }

                        if (dic.ContainsKey("params"))
                            dic["params"] += ("&" + temp);
                        else
                        {
                            dic.Add("params", temp);
                        }
                    }
                }
                else if (item.Contains(":") && __validColonSign__(item))
                {
                    kv = __splite__(item);
                    kv[0] = kv[0].Trim();
                    kv[1] = kv[1].Trim();
                }
                else if (item.StartsWith("GET"))
                {
                    string t = item.Substring(3).Trim().Replace("HTTP/1.1", string.Empty);
                    if (t.Contains("?"))
                    {
                        string[] temp = t.Split('?');
                        t = temp[0].Trim();

                        dic.Add("params", temp[1]);
                    }
                    t = t.Replace(".php", ".py");
                    dic.Add("connection-type", "GET");
                    dic.Add("target", t);
                }
                else if (item.StartsWith("POST"))
                {
                    isPost = true;
                    string t = item.Substring(4).Replace("HTTP/1.1", string.Empty).Trim();
                    if (t.Contains("?"))
                    {
                        string[] temp = t.Split('?');
                        t = temp[0].Trim();

                        dic.Add("params", temp[1]);
                    }
                    t = t.Replace(".php", ".py");
                    dic.Add("connection-type", "POST");
                    dic.Add("target", t);
                }
                else
                    continue;
                
                if (kv != null)
                { dic.Add(kv[0], kv[1]);}
            }

            
            if (dic["target"].Trim().Equals("/") || dic["target"].Trim()[0] == '/')
                dic["target"] = string.Empty;
            dic["target"] = dic["target"].Replace('/', System.IO.Path.DirectorySeparatorChar);


        }

        public string GetValue(string key)
        {
            return dic.ContainsKey(key) ? dic[key] : null;
        }
        public void SetValue(string key, string value)
        {
            if (dic.ContainsKey(key))
                dic[key] = value;
            else
                dic.Add(key, value);
        }
        public void Delete(string key)
        {
            if (dic.ContainsKey(key))
                dic.Remove(key);
        }
        public Dictionary<string, string> GetCollection() => dic;
        



        private string[] __splite__(string input)
        {
            string[] output = { string.Empty, string.Empty };

            int pointer = 0;
            bool found = false;
            foreach(char c in input)
            {
                if(c == ':' && !found)
                {
                    pointer++;
                    found = true;
                    continue;
                }

                output[pointer] += c;
            }
            return output;
        }

        private bool __validColonSign__(string item)
        {
            bool a = false;
            foreach (char ch in item)
            {
                if(ch == ':') {
                    if (a) return false;
                    else return true;
                }

                if (ch == '(' || ch == '[' || ch == '{') a = true;
                if(ch == ')' || ch == ']' || ch == '}') a = false;
            }
            return true;
        }


        
        private string[] Content;
        private Dictionary<string, string> dic;

    }
}

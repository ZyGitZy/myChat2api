namespace chatgot.Models.MonicaModels
{
    public class MonicaRequest
    {
        public string task_uid { get; set; }
        public string bot_uid { get; set; }
        public Data data { get; set; }
        public string language { get; set; }
        public string locale { get; set; }
        public string task_type { get; set; }
        public ToolData tool_data { get; set; }
        public string ai_resp_language { get; set; }
    }

    public class Data
    {
        public string conversation_id { get; set; }
        public List<Item> Items { get; set; }
        public string pre_generated_reply_Id { get; set; }
        public string pre_parent_item_Id { get; set; }
        public string origin { get; set; }
        public string origin_page_title { get; set; }
        public string trigger_by { get; set; }
        public string usemodel { get; set; }
    }

    public class Item
    {
        public string item_Id { get; set; }
        public string conversation_id { get; set; }
        public string item_type { get; set; }
        public string summary { get; set; }
        public string parent_item_id { get; set; }
        public ItemData data { get; set; }
    }

    public class ItemData
    {
        public string type { get; set; }
        public string content { get; set; }
        public bool render_in_streaming { get; set; }
        public string quote_content { get; set; }
        public string question_id { get; set; }
    }

    public class ToolData
    {
        public List<SysSkill> sys_skill_list { get; set; }
    }

    public class SysSkill
    {
        public string uid { get; set; }
        public bool enable { get; set; }
    }

}

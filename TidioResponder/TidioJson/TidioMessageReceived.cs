using System.Collections.Generic;

public class Message
{
    public string type { get; set; }
    public string message { get; set; }
    public List<object> buttons { get; set; }
    public List<object> quick_replies { get; set; }
}

public class Data
{
    public bool is_waiting_for_answer { get; set; }
    public string auto { get; set; }
    public string browser_session_id { get; set; }
    public string channel { get; set; }
    public string connection_uuid { get; set; }
    public int id { get; set; }
    public object bot_id { get; set; }
    public Message message { get; set; }
    public object operator_id { get; set; }
    public string project_public_key { get; set; }
    public int time_displayed { get; set; }
    public int time_sent { get; set; }
    public string type { get; set; }
    public string visitor_email { get; set; }
    public string visitor_id { get; set; }
}

public class TidioMessage
{
    public string hash { get; set; }
    public string connection_uuid { get; set; }
    public Data data { get; set; }
    public List<object> operators { get; set; }
}
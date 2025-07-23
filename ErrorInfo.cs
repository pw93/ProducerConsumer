using System;


namespace ProfitWin.Common
{
    public class ErrorInfo
    {
        public int code { get; set; } = 0;
        public string msg1 { get; set; } = "";
        public string msg2 { get; set; } = "";

        public ErrorInfo() { }

        public ErrorInfo(int code1, string mg1, string mg2 = "")
        {
            code = code1;
            msg1 = mg1;
            msg2 = mg2;
        }       

        public void SetData(int code1, string mg1, string mg2 = "")
        {
            code = code1;
            msg1 = mg1;
            msg2 = mg2;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(msg1) && string.IsNullOrEmpty(msg2))
                return $"ErrorInfo:{{{code}|_}}";
            if (msg1 == msg2 || string.IsNullOrEmpty(msg2))
                return $"ErrorInfo:{{{code}|{msg1}}}";
            else
                return $"ErrorInfo:{{{code}|{msg1}|{msg2}}}";
        }


        public static ErrorInfo Ok => new ErrorInfo();  // 快速取得正常情況
        public static ErrorInfo QuickError(string msg1, string msg2 = "")
        {
            if (string.IsNullOrEmpty(msg2))
                return new ErrorInfo(-1, msg1, msg1);
            else
                return new ErrorInfo(-1, msg1, msg2);
        }

        public static ErrorInfo QuickError(string msg1, ErrorInfo sub)
        {
            return new ErrorInfo(-1, msg1, sub.msg2);
        }        

        public static ErrorInfo QuickException(Exception ex) =>
        new ErrorInfo(-1, "exception", $"ex: {ex.Message}");

        public bool IsOk => code == 0;
    }
}

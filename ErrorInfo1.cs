using System;


namespace test_uf_bot
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
            return $"ErrorInfo:{{{code}|{msg1}|{msg2}}}";
        }


        public static ErrorInfo Ok => new ErrorInfo();  // 快速取得正常情況
        public static ErrorInfo QuickError(string msg1, string msg2 = "") =>
        new ErrorInfo(-1, msg1, msg2);

        public static ErrorInfo QuickException(Exception ex) =>
        new ErrorInfo(-1, "exception", ex.Message);
    }
}

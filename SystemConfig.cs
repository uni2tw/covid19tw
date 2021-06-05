using System;

namespace Covid19TW
{
    public interface ISystemConfig
    {
        int ItemsQueryLimit { get; set; }
    }


    public class SystemConfig
    {
        public int ItemsQueryLimit { get; set; }
    }
}
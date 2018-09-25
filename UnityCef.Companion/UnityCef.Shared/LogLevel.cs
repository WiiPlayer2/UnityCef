using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public enum LogLevel
    {
        //
        // Summary:
        //     Default logging (currently INFO logging).
        Default = 0,
        //
        // Summary:
        //     Verbose logging.
        Verbose = 1,
        //
        // Summary:
        //     DEBUG logging.
        Debug = 1,
        //
        // Summary:
        //     INFO logging.
        Info = 2,
        //
        // Summary:
        //     WARNING logging.
        Warning = 3,
        //
        // Summary:
        //     ERROR logging.
        Error = 4,
        //
        // Summary:
        //     ERROR_REPORT logging.
        ErrorReport = 5,
        //
        // Summary:
        //     Completely disable logging.
        Disable = 99
    }
}

﻿using System.Diagnostics;
using TABS_Multiplayer;

#pragma warning disable CS0626 // Disable that annoying style warning
class patch_TABSBooter : TABSBooter
{
    private static bool inited = false; // Don't execute the init twice

    // Hook the init method
    public extern void orig_Init();
    public new void Init()
    {
        if (!inited)
        {
            inited = true;
            SocketConnection.Init(); // Init the socket manager
            Process.Start("TABS-Multiplayer-UI.exe"); // Start the multiplayer UI
        }

        orig_Init(); // Execute the real method
    }
}
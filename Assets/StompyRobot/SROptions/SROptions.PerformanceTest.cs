using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using SRDebugger;
using SRDebugger.Services;
using SRF;
using SRF.Service;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public partial class SROptions
{

    [Category("Quality Settings")]
    public void SwitchFog()
    {
        RenderSettings.fog = !RenderSettings.fog;
    }

    [Category("Quality Settings")]
    public void SwitchPixelLightCount()
    {
        if (QualitySettings.pixelLightCount == 0)
        {
            QualitySettings.pixelLightCount = 1;
        }
        else
        {
            QualitySettings.pixelLightCount = 0;
        }
    }
}

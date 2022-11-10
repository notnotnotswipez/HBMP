using System;
using System.Collections.Generic;
using System.Text;
using HBMP.DataType;
using MelonLoader;

namespace HBMP.Messages
{
    public enum NetworkMessageType : byte {
        PlayerUpdateMessage    = 0,
        ShortIdUpdateMessage = 1,
        TransformUpdateMessage = 2,
        InitializeSyncMessage = 3,
        OwnerChangeMessage = 4,
        DisconnectMessage = 5,
        GunshotMessage = 6,
        RequestIdsMessage = 7,
        ExplodeMessage = 8,
        JoinCatchupMessage = 9,
        EnemySpawnMessage = 10,
        EnemyDestroyMessage = 11,
        SceneTransferMessage = 12,
        IkUpdateMessage = 13,
        ModMessage = 14
    }
} 
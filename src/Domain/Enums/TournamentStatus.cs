using Domain.AggregateRoots;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Domain.Enums
{
    /// <summary>
    /// Explicit state machine for tournament lifecycle.
    /// </summary>
    public enum TournamentStatus
    {
        /// <summary> Setup state - Initial configuration, registration open</summary>
        Setup = 1,

        /// <summary> Transition state - Backend seeding groups (show loading)</summary>
        SeedingGroups = 2,

        /// <summary> Active state - Users can score group matches</summary>
        GroupsInProgress = 3,

        /// <summary> Waiting state - All groups finished, awaiting admin approval</summary>
        GroupsCompleted = 4,

        /// <summary> Transition state - Backend seeding bracket (show loading)</summary>
        SeedingBracket = 5,

        /// <summary> Active state - Users can score bracket matches</summary>
        BracketInProgress = 6,

        /// <summary> Terminal state - Final results declared, tournament complete</summary>
        Finished = 7,

        /// <summary>  Terminal state - Tournament cancelled/archived</summary>
        Cancelled = 8,
    }
}
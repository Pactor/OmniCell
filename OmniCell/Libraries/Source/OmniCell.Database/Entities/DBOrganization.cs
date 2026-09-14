#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace OmniCell.Database.Dao
{
    #region Usings ...

    using System;

    using OmniCell.Database.Entities;

    #endregion

    /// <summary>
    /// Data object for organization data
    /// </summary>
    [Tablename("organizations")]
    public class DBOrganization : IDBEntity
    {
        /// <summary>
        /// Creation date of the organization
        /// </summary>
        public DateTime Creation { get; set; }

        /// <summary>
        /// Name of the organization
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Id of organization President
        /// </summary>
        public int LeaderId { get; set; }

        /// <summary>
        /// Government form
        /// </summary>
        public int GovernmentForm { get; set; }

        /// <summary>
        /// Description of the organization
        /// </summary>
        /// <remarks>
        /// Every property goes into the INSERT, so a null here is a null in a
        /// NOT NULL column. Same for Objective and History.
        /// </remarks>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Organization's objective
        /// </summary>
        public string Objective { get; set; } = string.Empty;

        /// <summary>
        /// Organizations history
        /// </summary>
        public string History { get; set; } = string.Empty;

        /// <summary>
        /// Tax rate
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// Organization bank id
        /// </summary>
        public ulong Bank { get; set; }

        /// <summary>
        /// Organization commission
        /// </summary>
        /// <remarks>
        /// Spelled as the column is in SqlTables/organizations.sql and so in
        /// every database created from it. Named Commission, the INSERT failed
        /// with "Unknown column 'Commission'". The table defaults it to 1.
        /// </remarks>
        public int Comission { get; set; } = 1;

        /// <summary>
        /// Id of the organizations contracts
        /// </summary>
        public int ContractsId { get; set; }

        /// <summary>
        /// Organization City id
        /// </summary>
        public int CityId { get; set; }

        /// <summary>
        /// Tower field id
        /// </summary>
        public int TowerFieldId { get; set; }

        /// <summary>
        /// Organization id   
        /// </summary>
        public int Id { get; set; }
    }
}
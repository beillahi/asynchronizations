#region LICENSE

/*
    
    Copyright(c) Voat, Inc.

    This file is part of Voat.

    This source file is subject to version 3 of the GPL license,
    that is bundled with this package in the file LICENSE, and is
    available online at http://www.gnu.org/licenses/gpl-3.0.txt;
    you may not use this file except in compliance with the License.

    Software distributed under the License is distributed on an
    "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express
    or implied. See the License for the specific language governing
    rights and limitations under the License.

    All Rights Reserved.

*/

#endregion LICENSE

using System;

namespace Voat.Models.API.Legacy
{
    public class ApiComment
    {
        public string CommentContent { get; set; }

        public DateTime Date { get; set; }

        public int Dislikes { get; set; }

        public int Id { get; set; }

        public Nullable<DateTime> LastEditDate { get; set; }

        public int Likes { get; set; }

        public Nullable<int> MessageId { get; set; }

        public string Name { get; set; }

        public Nullable<int> ParentId { get; set; }
    }
}

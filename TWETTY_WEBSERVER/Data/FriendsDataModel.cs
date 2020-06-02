using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TWETTY_WEBSERVER
{
    /// <summary>
    /// Our Settings database table representational model
    /// </summary>
    public class FriendsDataModel
    {
        /// <summary>
        /// The unique Id for this entry
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The settings name
        /// </summary>
        /// <remarks>This column is indexed</remarks>
        [Required]
        [MaxLength(450)]
        public string RequestedBy_Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string RequestedTo_Id { get; set; }

        /// <summary>
        /// The settings value
        /// </summary>
        [Required]
        public int FriendRequestFlag { get; set; }
    }
}

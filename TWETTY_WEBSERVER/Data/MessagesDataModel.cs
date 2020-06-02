using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TWETTY_WEBSERVER
{
    /// <summary>
    /// Our Settings database table representational model
    /// </summary>
    public class MessagesDataModel
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
        public string SendBy_Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string SendTo_Id { get; set; }

        [Required]
        public String Message { get; set; }

        [Required]
        public DateTimeOffset MessageSentTime { get; set; }

    }
}

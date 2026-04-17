using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DR.Data;

[Table("tracks")]
public class Track
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Artist { get; set; } = string.Empty;

    [Required]
    [Column("playedAt")]
    public DateTime PlayedAt { get; set; }

    [Required]
    [Column("channel")]
    public string Channel { get; set; } = string.Empty;
}
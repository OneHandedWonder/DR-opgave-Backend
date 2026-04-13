using System.ComponentModel.DataAnnotations;
namespace DR.Data;

public class Record
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int ReleaseYear { get; set; }

    [Required]
    public string Genre { get; set; } = string.Empty;

    [Required]
    public string Artist { get; set; } = string.Empty;

    [Required]
    public int trackCount { get; set; }

    [Required]
    public int Duration { get; set; }
}
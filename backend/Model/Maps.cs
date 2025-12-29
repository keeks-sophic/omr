using System.Collections.Generic;

namespace Backend.Model;

public class Maps
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Nodes>? Nodes { get; set; }
    public ICollection<Paths>? Paths { get; set; }
    public ICollection<Points>? Points { get; set; }
    public ICollection<Qrs>? Qrs { get; set; }
}

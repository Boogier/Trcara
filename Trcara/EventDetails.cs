internal readonly record struct EventDetails(
    string Type, 
    string Title, 
    string? Distance,
    string? Elevation,
    string Date, 
    string Link, 
    string Facebook, 
    string Instagram, 
    string? Deadline, 
    string? Contact, 
    string Country,
    string Location);

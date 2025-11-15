internal readonly record struct EventDetails(
    string Type, 
    string Title, 
    string Date, 
    string Link, 
    string Facebook, 
    string Instagram, 
    string? Deadline, 
    string? Contact, 
    string Country,
    string Location);

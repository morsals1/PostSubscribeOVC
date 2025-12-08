using WpfSUB.Models;
using WpfSUB;

public class ClientProfile : ObservableObject
{
    private int _id;
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private int _clientId;
    public int ClientId
    {
        get => _clientId;
        set => SetProperty(ref _clientId, value);
    }

    private Client _client;
    public Client Client
    {
        get => _client;
        set => SetProperty(ref _client, value);
    }

    private string _avatarUrl = ""; // Значение по умолчанию
    public string AvatarUrl
    {
        get => _avatarUrl;
        set => SetProperty(ref _avatarUrl, value);
    }

    private string _phone = ""; // Значение по умолчанию
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    private DateTime? _birthday;
    public DateTime? Birthday
    {
        get => _birthday;
        set => SetProperty(ref _birthday, value);
    }

    private string _bio = ""; // Значение по умолчанию
    public string Bio
    {
        get => _bio;
        set => SetProperty(ref _bio, value);
    }

    private string _preferences = ""; // Значение по умолчанию
    public string Preferences
    {
        get => _preferences;
        set => SetProperty(ref _preferences, value);
    }
}
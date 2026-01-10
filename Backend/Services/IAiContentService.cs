namespace Backend.Services
{
    public interface IAiContentService
    {
        // Returnează TRUE dacă textul e curat, FALSE dacă e nepotrivit
        Task<bool> IsContentSafeAsync(string content);
    }
}
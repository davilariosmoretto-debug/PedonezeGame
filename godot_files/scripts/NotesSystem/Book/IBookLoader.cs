namespace BookApiFile;

public interface IBookLoader
{
    BookDocument Load(string directory);
}


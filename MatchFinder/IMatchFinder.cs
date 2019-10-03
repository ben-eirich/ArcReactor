namespace ArcReactor {
    public interface IMatchFinder {
        bool FindFirst(int inputOffset, out int length, out int distance);
        bool FindNext(out int length, out int distance);
    }
}

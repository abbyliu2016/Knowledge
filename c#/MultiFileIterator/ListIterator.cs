public class ListIterator<T> : IIterator<T, int>
{
    private List<T> list;
  //  private int index;

   // private int? stateIdx;
    private IState<int> stateIdx = new IdxState();

    public ListIterator(List<T> list)
    {
        this.list = list;
        this.stateIdx.Value = 0;
    }

    private bool HasNext()
    {
        return stateIdx.Value < list.Count;
    }

    public T Next()
    {
        if (HasNext())
        { 
            return list[stateIdx.Value++];
        }

        throw new InvalidOperationException("No more element in the list");

    }

    public IState<int> SaveState()
    {
        return stateIdx;
    }

    public void SetState(IState<int> idx)
    {
    stateIdx = idx;
    }
}

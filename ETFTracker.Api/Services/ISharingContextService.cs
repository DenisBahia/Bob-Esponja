namespace ETFTracker.Api.Services;
public interface ISharingContextService
{
    int GetEffectiveUserId();
    bool IsReadOnly();
    bool IsViewingAsOther();
}

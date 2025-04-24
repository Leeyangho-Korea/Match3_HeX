using System.Collections;

public interface IObstacle
{
    void OnNearbyMatch();               // 매칭 주변에서 영향을 받을 때
    bool IsRemovable { get; }           // 제거 조건 충족 여부
    IEnumerator PlayDestroyEffect();    // 제거 연출
}

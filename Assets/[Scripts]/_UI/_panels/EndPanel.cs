namespace GAITemplate
{
    /// <summary>
    /// SuccessPanel ile FailPanel'i tek bir UIManager girişiyle (endPanel.Success/Fail) çağırmak için
    /// dispatcher. Kendi visual'ı yok — sadece doğru alt paneli açar.
    ///
    /// Sahnedeki yapı:
    ///   EndPanel (CanvasGroup, bu script)
    ///   ├── Success (SuccessPanel script)
    ///   └── Fail    (FailPanel script)
    /// </summary>
    public class EndPanel : Panel
    {
        public SuccessPanel success;
        public FailPanel    fail;

        public void Success()
        {
            if (fail != null)
                fail.gameObject.SetActive(false);

            if (success != null)
                success.Show();
        }

        public void Fail()
        {
            if (success != null)
                success.gameObject.SetActive(false);

            if (fail != null)
                fail.Show();
        }
    }
}

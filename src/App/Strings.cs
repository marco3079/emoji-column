// Copyright (c) marco3079. Licensed under the MIT License.
using System.Globalization;

namespace EmojiColumn.App
{
    /// <summary>Tiny two-language string table (Portuguese when the UI language is pt-*, English otherwise).</summary>
    internal static class S
    {
        public static readonly bool Pt = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt";

        private static string T(string en, string pt) { return Pt ? pt : en; }

        public static string AppName { get { return "Emoji Column"; } }
        public const string RepoUrl = "https://github.com/marco3079/emoji-column";

        // Main window
        public static string Tagline { get { return T("Add an \"Emoji\" column to Windows File Explorer.", "Adicione uma coluna \"Emoji\" ao Explorador de Arquivos do Windows."); } }
        public static string StatusInstalled { get { return T("Installed", "Instalado"); } }
        public static string StatusNotInstalled { get { return T("Not installed", "Não instalado"); } }
        public static string FileTypes { get { return T("File types that show the Emoji column", "Tipos de arquivo que mostram a coluna Emoji"); } }
        public static string Chained { get { return T("keeps its existing handler", "mantém o handler existente"); } }
        public static string AddType { get { return T("Add", "Adicionar"); } }
        public static string Install { get { return T("Install / Update", "Instalar / Atualizar"); } }
        public static string Uninstall { get { return T("Uninstall", "Desinstalar"); } }
        public static string RestartExplorer { get { return T("Restart Explorer", "Reiniciar Explorer"); } }
        public static string HowTo { get { return T("How to show the column", "Como mostrar a coluna"); } }
        public static string TagFiles { get { return T("Tag files…", "Marcar arquivos…"); } }
        public static string AdminNote { get { return T("Installing requires administrator rights.", "Instalar exige permissão de administrador."); } }

        public static string HowToText
        {
            get
            {
                return T(
                    "1. Open a folder in File Explorer and switch to Details view (View > Details).\n" +
                    "2. Right-click any column header (Name, Date modified…) and choose \"More…\".\n" +
                    "3. Tick \"Emoji\" and click OK. Drag the column where you want it.\n\n" +
                    "To tag files: right-click them > Show more options > Emoji.\n" +
                    "The \"My description\" column works the same way (Emoji > Description…), and can also be edited in the Details pane (Alt+Shift+P).\n\n" +
                    "Tip: to reuse the layout in every folder of the same kind, use View > Options > View > \"Apply to Folders\".",
                    "1. Abra uma pasta no Explorador de Arquivos no modo Detalhes (Exibir > Detalhes).\n" +
                    "2. Clique com o botão direito no cabeçalho de qualquer coluna (Nome, Data de modificação…) e escolha \"Mais…\".\n" +
                    "3. Marque \"Emoji\" e clique em OK. Arraste a coluna para onde quiser.\n\n" +
                    "Para marcar arquivos: botão direito > Mostrar mais opções > Emoji.\n" +
                    "A coluna \"Minha descrição\" funciona igual (Emoji > Descrição…) e também pode ser editada no painel de detalhes (Alt+Shift+P).\n\n" +
                    "Dica: para usar o mesmo layout em todas as pastas do mesmo tipo, use Opções > Modo de Exibição > \"Aplicar às Pastas\".");
            }
        }

        public static string InstallDone { get { return T("Emoji Column is installed.\n\nRestart Explorer now so the column shows up?", "Emoji Column foi instalado.\n\nReiniciar o Explorer agora para a coluna aparecer?"); } }
        public static string UninstallConfirm { get { return T("Remove Emoji Column from this computer?\n\nThe emojis already set stay stored in the files and come back if you reinstall.", "Remover o Emoji Column deste computador?\n\nOs emojis já marcados continuam guardados nos arquivos e voltam se você reinstalar."); } }
        public static string UninstallDone { get { return T("Emoji Column was removed. Restart Explorer to finish?", "Emoji Column foi removido. Reiniciar o Explorer para concluir?"); } }
        public static string RestartConfirm { get { return T("Restarting Explorer closes all open File Explorer windows. Continue?", "Reiniciar o Explorer fecha todas as janelas do Explorador de Arquivos abertas. Continuar?"); } }
        public static string NoTypes { get { return T("Select at least one file type.", "Selecione pelo menos um tipo de arquivo."); } }
        public static string InvalidType { get { return T("Type an extension such as .csv", "Digite uma extensão como .csv"); } }
        public static string Failed { get { return T("Operation failed:", "A operação falhou:"); } }
        public static string Cancelled { get { return T("Cancelled.", "Cancelado."); } }

        // Context menu
        public static string MenuRoot { get { return "Emoji"; } }
        public static string MenuChoose { get { return T("Choose emoji…", "Escolher emoji…"); } }
        public static string MenuRemove { get { return T("Remove emoji", "Remover emoji"); } }
        public static string MenuDescription { get { return T("Description…", "Descrição…"); } }

        // Description
        public static string DescriptionColumn { get { return T("My description", "Minha descrição"); } }
        public static string EmojiTextColumn { get { return T("Emoji (text)", "Emoji (texto)"); } }
        public static string DescTitle(int count)
        {
            return count == 1
                ? T("Description of 1 file", "Descrição de 1 arquivo")
                : string.Format(T("Description of {0} files", "Descrição de {0} arquivos"), count);
        }
        public static string DescHint { get { return T("Shown in the \"My description\" column. Leave empty to remove.", "Aparece na coluna \"Minha descrição\". Deixe vazio para remover."); } }
        public static string DescMixed { get { return T("The selected files have different descriptions; saving replaces all of them.", "Os arquivos selecionados têm descrições diferentes; salvar substitui todas."); } }
        public static string Save { get { return T("Save", "Salvar"); } }
        public static string Cancel { get { return T("Cancel", "Cancelar"); } }

        // Picker
        public static string PickTitle(int count)
        {
            return count == 1
                ? T("Choose an emoji for 1 file", "Escolha um emoji para 1 arquivo")
                : string.Format(T("Choose an emoji for {0} files", "Escolha um emoji para {0} arquivos"), count);
        }
        public static string Custom { get { return T("Custom (Win + . opens the emoji panel; only the emojis above show in color):", "Personalizado (Win + . abre o painel; só os emojis acima aparecem coloridos):"); } }
        public static string Apply { get { return T("Apply", "Aplicar"); } }
        public static string Remove { get { return T("Remove emoji", "Remover emoji"); } }
        public static string Close { get { return T("Close", "Fechar"); } }
        public static string NoFiles { get { return T("No files selected.", "Nenhum arquivo selecionado."); } }
        public static string NotEnabled { get { return T("The Emoji column is not enabled for: {0}. The emoji is saved, but it only shows after you enable these types.", "A coluna Emoji não está ativada para: {0}. O emoji é salvo, mas só aparece depois de ativar esses tipos."); } }

        // CLI
        public static string Usage
        {
            get
            {
                return "Emoji Column\n\n" +
                       "Usage:\n" +
                       "  EmojiColumn.exe                          open the manager window\n" +
                       "  EmojiColumn.exe pick  <files...>         open the emoji picker\n" +
                       "  EmojiColumn.exe set   <emoji> <files...> set the emoji\n" +
                       "  EmojiColumn.exe clear <files...>         remove the emoji\n" +
                       "  EmojiColumn.exe get   <files...>         print emoji and description\n" +
                       "  EmojiColumn.exe set-desc <text> <files...>  set the description\n" +
                       "  EmojiColumn.exe clear-desc <files...>    remove the description\n" +
                       "  EmojiColumn.exe edit-desc  <files...>    open the description editor\n" +
                       "  EmojiColumn.exe install [--ext .csv,.pdf] [--quiet]\n" +
                       "  EmojiColumn.exe uninstall [--quiet]\n";
            }
        }
    }
}

#include <atlcomcli.h>

#include "ContextMenuCommand.h"

ContextMenuCommand::ContextMenuCommand(std::wofstream& logFile, const ContextMenuEntry contextMenuEntry) : logFile(logFile), contextMenuEntry(contextMenuEntry) {
  if (logFile.is_open()) {
    logFile << L"Initializing context menu command" << std::endl;
  }
}

std::wstring ContextMenuCommand::QuoteIfNeeded(const std::wstring& s) {
  // Simple quote wrapper (assumes already safe content)
  std::wstring result = L"\"";
  result += s;
  result += L"\"";
  return result;
}

std::wstring ContextMenuCommand::ExpandCommand(const std::wstring& command, IShellItemArray* psiArray) {
  std::wstring result = command;

  std::wstring firstPath;
  std::wstring allPaths;

  DWORD count = 0;

  if (psiArray && SUCCEEDED(psiArray->GetCount(&count))) {
    for (DWORD i = 0; i < count; ++i) {
      CComPtr<IShellItem> pItem;

      if (SUCCEEDED(psiArray->GetItemAt(i, &pItem))) {
        LPWSTR pszPath = nullptr;

        if (SUCCEEDED(pItem->GetDisplayName(SIGDN_FILESYSPATH, &pszPath))) {
          std::wstring quoted = QuoteIfNeeded(pszPath);

          if (i == 0) firstPath = quoted;

          allPaths.append(quoted);

          if (i + 1 < count) allPaths.append(L" ");

          CoTaskMemFree(pszPath);
        }
      }
    }
  }

  // Substitute %1 (first item) and %* (all items)
  size_t pos;

  while ((pos = result.find(L"%1")) != std::wstring::npos) {
    result.replace(pos, 2, firstPath);
  }

  while ((pos = result.find(L"%*")) != std::wstring::npos) {
    result.replace(pos, 2, allPaths);
  }

  return result;
}

std::wstring ContextMenuCommand::GetDirectoryFromFirstItem(IShellItemArray* psiArray) {
  if (!psiArray) return L"";

  CComPtr<IShellItem> pItem;

  if (SUCCEEDED(psiArray->GetItemAt(0, &pItem))) {
    LPWSTR pszPath = nullptr;

    if (SUCCEEDED(pItem->GetDisplayName(SIGDN_FILESYSPATH, &pszPath))) {
      std::wstring fullPath(pszPath);
      CoTaskMemFree(pszPath);

      size_t lastSlash = fullPath.find_last_of(L"\\/");

      if (lastSlash != std::wstring::npos) {
        return fullPath.substr(0, lastSlash);
      }
    }
  }
  return L"";
}

bool ContextMenuCommand::Launch(std::wstring currentDirectory, std::wstring command) {
  STARTUPINFOW si = { sizeof(si) };
  PROCESS_INFORMATION pi = {};

  BOOL success = CreateProcessW(
    nullptr,
    &command[0],
    nullptr,
    nullptr,
    FALSE,
    0,
    nullptr,
    currentDirectory.c_str(),
    &si,
    &pi
  );

  if (success) {
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);

    if (logFile.is_open()) {
      logFile << L"Launched in " << currentDirectory << L": " << command << std::endl;
    }

    return true;
  }

  if (logFile.is_open()) {
    logFile << L"ERROR: CreateProcessW failed: " << GetLastError() << std::endl;
  }

  return false;
}

IFACEMETHODIMP ContextMenuCommand::QueryInterface(REFIID riid, void** ppv) {
  if (!ppv) return E_POINTER;

  *ppv = nullptr;

  if (riid == IID_IUnknown || riid == IID_IExplorerCommand) {
    *ppv = static_cast<IExplorerCommand*>(this);
    AddRef();

    return S_OK;
  }

  return E_NOINTERFACE;
}

IFACEMETHODIMP_(ULONG) ContextMenuCommand::AddRef() {
  return InterlockedIncrement(&refCount);
}

IFACEMETHODIMP_(ULONG) ContextMenuCommand::Release() {
  ULONG count = InterlockedDecrement(&refCount);

  if (!count) delete this;

  return count;
}

IFACEMETHODIMP ContextMenuCommand::GetTitle(IShellItemArray*, LPWSTR* ppszName) {
  *ppszName = _wcsdup(contextMenuEntry.title.c_str());

  return S_OK;
}

IFACEMETHODIMP ContextMenuCommand::GetIcon(IShellItemArray*, LPWSTR* ppszIcon) {
  *ppszIcon = _wcsdup(contextMenuEntry.icon.c_str());

  return S_OK;
}

IFACEMETHODIMP ContextMenuCommand::GetToolTip(IShellItemArray*, LPWSTR* ppszTip) {
  *ppszTip = _wcsdup(contextMenuEntry.toolTip.c_str());

  return S_OK;
}

IFACEMETHODIMP ContextMenuCommand::GetCanonicalName(GUID* pguidCommandName) {
  *pguidCommandName = contextMenuEntry.clsid;

  return S_OK;
}

IFACEMETHODIMP ContextMenuCommand::GetState(IShellItemArray*, BOOL, EXPCMDSTATE* pCmdState) {
  *pCmdState = ECS_ENABLED;

  return S_OK;
}

IFACEMETHODIMP ContextMenuCommand::Invoke(IShellItemArray* psiItemArray, IBindCtx*) {
  std::wstring wCommand(ExpandCommand(contextMenuEntry.command, psiItemArray));

  return Launch(GetDirectoryFromFirstItem(psiItemArray), wCommand.c_str()) ? E_UNEXPECTED : S_OK;
}

IFACEMETHODIMP ContextMenuCommand::GetFlags(EXPCMDFLAGS* pFlags) {
  *pFlags = 0;
  
  return S_OK;
}

IFACEMETHODIMP ContextMenuCommand::EnumSubCommands(IEnumExplorerCommand** ppEnum) {
  *ppEnum = nullptr;
  
  return E_NOTIMPL;
}
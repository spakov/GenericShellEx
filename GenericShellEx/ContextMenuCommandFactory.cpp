#include "ContextMenuCommand.h"

#include "ContextMenuCommandFactory.h"

ContextMenuCommandFactory::ContextMenuCommandFactory(std::wofstream& logFile, ContextMenuEntry contextMenuEntry) : logFile(logFile), contextMenuEntry(contextMenuEntry) {
  if (logFile.is_open()) {
    logFile << L"Initializing context menu command factory" << std::endl;
  }
}

IFACEMETHODIMP ContextMenuCommandFactory::QueryInterface(REFIID riid, void** ppv) {
  if (!ppv) return E_POINTER;
  *ppv = nullptr;

  if (riid == IID_IUnknown || riid == IID_IClassFactory) {
    *ppv = static_cast<IClassFactory*>(this);
    AddRef();

    return S_OK;
  }

  return E_NOINTERFACE;
}

IFACEMETHODIMP_(ULONG) ContextMenuCommandFactory::AddRef() {
  return InterlockedIncrement(&refCount);
}

IFACEMETHODIMP_(ULONG) ContextMenuCommandFactory::Release() {
  ULONG count = InterlockedDecrement(&refCount);

  if (!count) delete this;

  return count;
}

IFACEMETHODIMP ContextMenuCommandFactory::CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv) {
  if (pUnkOuter) return CLASS_E_NOAGGREGATION;

  auto* provider = new (std::nothrow) ContextMenuCommand(logFile, contextMenuEntry);

  if (!provider) return E_OUTOFMEMORY;

  HRESULT hr = provider->QueryInterface(riid, ppv);
  provider->Release();

  return hr;
}

IFACEMETHODIMP ContextMenuCommandFactory::LockServer(BOOL) {
  return S_OK;
}

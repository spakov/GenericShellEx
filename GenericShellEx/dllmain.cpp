#include <ShlObj_core.h>
#include <initguid.h>
#include "nlohmann/json.hpp"
#include "guid.h"
#include "ContextMenuCommandFactory.h"

extern "C" IMAGE_DOS_HEADER __ImageBase;

// Global DLL reference count
LONG g_cRefModule = 0;

std::wofstream g_logFile;

/// <summary>
/// The map of context menu entries.
/// </summary>
std::unordered_map<std::wstring, ContextMenuEntry> g_contextMenuEntries;

/// <summary>
/// Gets a <see cref="std::ifstream"/> for the configuration file.
/// </summary>
/// <returns>The configuration file.</returns>
extern std::ifstream GetConfigFileStream() {
  PWSTR localAppDataPath = nullptr;

  if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, NULL, &localAppDataPath))) {
    return std::ifstream();
  }

  std::wstring configPath(localAppDataPath);
  configPath.append(L"\\GenericShellEx\\config.json");

  CoTaskMemFree(localAppDataPath);

  std::ifstream configFile(configPath);

  return configFile;
}

/// <summary>
/// Expands environment variables in <paramref name="s"/>.
/// </summary>
/// <param name="s">The string in which to expand environment
/// variables.</param>
/// <returns><paramref name="s"/>, with environment variables
/// expanded.</returns>
extern std::wstring ExpandEnvVars(const std::wstring& s) {
  DWORD size = ExpandEnvironmentStringsW(s.c_str(), nullptr, 0);
  std::wstring result(size, L'\0');

  if (ExpandEnvironmentStringsW(s.c_str(), &result[0], size)) {
    // Remove the null terminator from std::wstring
    result.resize(static_cast<std::basic_string<wchar_t, std::char_traits<wchar_t>, std::allocator<wchar_t>>::size_type>(size) - 1);
    return result;
  }

  return L"";
}

/// <summary>
/// Converts a <see cref="std::string"/> to a <see cref="std::wstring"/>.
/// </summary>
/// <param name="s">The string to convert.</param>
/// <returns>A <see cref="std::wstring"/>.</returns>
extern std::wstring ConvertToWString(const std::string& s) {
  return std::wstring(s.begin(), s.end());
}

/// <summary>
/// Adds a context command to the map.
/// </summary>
/// <param name="wType">The type this context command is associated
/// with.</param>
/// <param name="entry">The JSON object that contains the context command's
/// definition.</param>
extern void AddContextCommand(const std::wstring& wType, const nlohmann::json& entry) {
  ContextMenuEntry contextMenuEntry;

  if (entry.contains("title")) {
    contextMenuEntry.title = ConvertToWString(entry["title"].get<std::string>());
  }

  if (entry.contains("toolTip")) {
    contextMenuEntry.toolTip = ConvertToWString(entry["toolTip"].get<std::string>());
  }

  if (entry.contains("icon")) {
    contextMenuEntry.icon = ConvertToWString(entry["icon"].get<std::string>());
  }

  if (entry.contains("command")) {
    contextMenuEntry.command = ConvertToWString(entry["command"].get<std::string>());
  }

  g_contextMenuEntries[wType] = contextMenuEntry;
}

extern HRESULT GetContextMenuCommandFactory(CLSID clsid, REFIID riid, void** ppv) {
  std::wstring wType;

  if (clsid == CLSID_StarContextMenuProvider) {
    wType = L"*";
  } else if (clsid == CLSID_DirectoryContextMenuProvider) {
    wType = L"Directory";
  } else if (clsid == CLSID_DirectoryBackgroundContextMenuProvider) {
    wType = L"Directory\\Background";
  } else {
    if (g_logFile.is_open()) {
      g_logFile << L"ERROR: Unable to map CLSID to type" << std::endl;
    }

    return CLASS_E_CLASSNOTAVAILABLE;
  }

  if (g_logFile.is_open()) {
    g_logFile << L"CLSID refers to " << wType << std::endl;
  }

  auto contextMenuEntry = g_contextMenuEntries.find(wType);

  if (contextMenuEntry == g_contextMenuEntries.end()) {
    if (g_logFile.is_open()) {
      g_logFile << L"ERROR: Config file does not define type " << wType << std::endl;
    }

    CLASS_E_CLASSNOTAVAILABLE;
  }

  contextMenuEntry->second.clsid = clsid;

  auto* factory = new (std::nothrow) ContextMenuCommandFactory(g_logFile, contextMenuEntry->second);

  if (!factory) return E_OUTOFMEMORY;

  HRESULT hr = factory->QueryInterface(riid, ppv);
  factory->Release();

  return hr;
}

_Check_return_
/// <summary>
/// Retrieves the class object from a DLL object handler or object application.
/// </summary>
/// <param name="rclsid">The <c>CLSID</c> that will associate the correct data
/// and code.</param>
/// <param name="riid">A reference to the identifier of the interface that the
/// caller is to use to communicate with the class object. Usually, this is
/// <c>IID_IClassFactory</c> (defined in the OLE headers as the interface
/// identifier for <c>IClassFactory</c>).</param>
/// <param name="ppv">The address of a pointer variable that receives the
/// interface pointer requested in <paramref name="riid"/>. Upon successful
/// return, <c>*</c><paramref name="ppv"/> contains the requested interface
/// pointer. If an error occurs, the interface pointer is <c>NULL</c>.</param>
/// <returns>This function can return the standard return values
/// <c>E_INVALIDARG</c>, <c>E_OUTOFMEMORY</c>, and <c>E_UNEXPECTED</c>, as well
/// as <c>S_OK</c> and <c>CLASS_E_CLASSNOTAVAILABLE</c>.</returns>
extern "C" HRESULT __stdcall DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ void** ppv) {
  std::ifstream configStream = GetConfigFileStream();

  if (configStream.is_open()) {
    nlohmann::json config;

    configStream >> config;

    if (config.contains("logFile") && config["logFile"].is_string()) {
      std::wstring wLogFile(config["logFile"].get<std::string>().begin(), config["logFile"].get<std::string>().end());
      wLogFile = ExpandEnvVars(wLogFile);
      g_logFile.open(wLogFile.c_str(), std::ofstream::out | std::ios_base::app);

      if (g_logFile.is_open()) {
        std::time_t now = std::time(nullptr);
#pragma warning(suppress : 4996)
        g_logFile << std::endl << L"[" << std::put_time(std::localtime(&now), L"%F %T") << L"]" << std::endl;
      }
    }

    if (config.contains("types") && config["types"].is_object()) {
      for (const auto& entry : config["types"].items()) {
        std::wstring wType(entry.key().begin(), entry.key().end());

        if (entry.value().is_object()) {
          AddContextCommand(wType, entry.value());
        }

        // This, sadly, is not supported in any meaningfully feasible way via
        // MSIX
        /*else if (entry.value().is_array()) {
          for (const auto& type : entry.value().items()) {
            if (type.value().is_object()) {
              AddContextCommand(wType, type.value());
            }
          }
        }*/
      }
    }

    if (g_logFile.is_open()) {
      LPOLESTR clsidString = nullptr;

      if (!FAILED(StringFromCLSID(rclsid, &clsidString))) {
        g_logFile << L"Initialized class object for CLSID " << clsidString << std::endl;
      }

      CoTaskMemFree(clsidString);
    }
  }

  return GetContextMenuCommandFactory(rclsid, riid, ppv);
}

__control_entrypoint(DllExport)
/// <summary>
/// Determines whether the DLL that implements this function is in use. If not,
/// the caller can unload the DLL from memory.
/// </summary>
/// <returns>If the function succeeds, the return value is <c>S_OK</c>.
/// Otherwise, it is <c>S_FALSE</c>.</returns>
extern "C" HRESULT __stdcall DllCanUnloadNow(void) {
  return (g_cRefModule == 0) ? S_OK : S_FALSE;
}

/// <summary>
/// An optional entry point into a dynamic-link library (DLL). When the system
/// starts or terminates a process or thread, it calls the entry-point function
/// for each loaded DLL using the first thread of the process. The system also
/// calls the entry-point function for a DLL when it is loaded or unloaded
/// using the <c>LoadLibrary</c> and <c>FreeLibrary</c> functions.
/// </summary>
/// <param name="hinstDLL">A handle to the DLL module. The value is the base
/// address of the DLL. The <c>HINSTANCE</c> of a DLL is the same as the
/// <c>HMODULE</c> of the DLL, so <paramref name="hinstDLL"/> can be used in
/// calls to functions that require a module handle.</param>
/// <param name="fdwReason">The reason code that indicates why the DLL
/// entry-point function is being called. This parameter can be one of the
/// following values: <c>DLL_PROCESS_ATTACH</c>, <c>DLL_PROCESS_DETACH</c>,
/// <c>DLL_THREAD_ATTACH</c>, and <c>DLL_THREAD_DETACH</c>.</param>
/// <returns>The function returns <c>TRUE</c> if it succeeds or <c>FALSE</c>
/// if initialization fails.</returns>
extern "C" BOOL APIENTRY DllMain(HMODULE hinstDLL, DWORD fdwReason, LPVOID /*lpvReserved*/) {
  if (fdwReason == DLL_PROCESS_ATTACH) DisableThreadLibraryCalls(hinstDLL);

  return TRUE;
}
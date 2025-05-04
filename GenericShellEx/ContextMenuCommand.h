#pragma once

#include <ShObjIdl_core.h>
#include <fstream>
#include "ContextMenuEntry.h"

/// <summary>
/// A context menu command.
/// </summary>
class ContextMenuCommand : public IExplorerCommand {
private:
  long refCount = 1;

  ContextMenuEntry contextMenuEntry;

  std::wofstream& logFile;

public:
  /// <summary>
  /// Initializes a <see cref="ContextMenuCommand"/>.
  /// </summary>
  /// <param name="logFile">A log file <see cref="std::wofstream"/>.</param>
  /// <param name="contextMenuEntry">The context menu entry to present.</param>
  ContextMenuCommand(std::wofstream& logFile, const ContextMenuEntry contextMenuEntry);

  /// <summary>
  /// Wraps a string in double quotes.
  /// </summary>
  /// <param name="str">The string to wrap.</param>
  /// <returns>A quoted string.</returns>
  std::wstring QuoteIfNeeded(const std::wstring& s);

  /// <summary>
  /// Handles expansion of <c>%1</c> and <c>%*</c> in commands.
  /// </summary>
  /// <remarks>
  /// <c>%1</c> is replaced with the quoted first shell item. <c>%*</c> is
  /// replaced with all shell items, quoted.
  /// </remarks>
  /// <param name="command">The command in which to expand <c>%1</c> and
  /// <c>%*</c>.</param>
  /// <param name="psiArray">The shell items array.</param>
  /// <returns>Expanded command.</returns>
  std::wstring ExpandCommand(const std::wstring& command, IShellItemArray* psiArray);

  /// <summary>
  /// Gets the directory containing the first shell item (or the shell item
  /// itself, if it's a directory).
  /// </summary>
  /// <param name="psiArray">The shell items array.</param>
  /// <returns>A directory.</returns>
  std::wstring GetDirectoryFromFirstItem(IShellItemArray* psiArray);

  /// <summary>
  /// Launches the command.
  /// </summary>
  /// <param name="currentDirectory">The directory in which the process
  /// should execute.</param>
  /// <param name="command">The command to execute.</param>
  /// <returns><c>true</c> on success or <c>false</c> otherwise.</returns>
  bool Launch(std::wstring currentDirectory, std::wstring command);

  /// <summary>
  /// Implements <see cref="IUnknown::QueryInterface"/>.
  /// </summary>
  /// <remarks>
  /// Queries a COM object for a pointer to one of its interface; identifying
  /// the interface by a reference to its interface identifier (IID). If the
  /// COM object implements the interface, then it returns a pointer to that
  /// interface after calling IUnknown::AddRef on it.
  /// </remarks>
  /// <param name="riid">A reference to the interface identifier (IID) of the
  /// interface being queried for.</param>
  /// <param name="ppv">The address of a pointer to an interface with the IID
  /// specified in the <paramref name="riid"/> parameter.</param>
  /// <returns>Returns <c>S_OK</c> if the requested interface was found in
  /// the table or if the requested interface was <c>IUnknown</c>. Returns
  /// <c>E_NOINTERFACE</c> if the requested interface was not
  /// found.</returns>
  IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv);

  /// <summary>
  /// Implements <see cref="IUnknown::AddRef"/>.
  /// </summary>
  /// <remarks>
  /// Increments the reference count for an interface pointer to a COM
  /// object. You should call this method whenever you make a copy of an
  /// interface pointer.
  /// </remarks>
  /// <returns>The method returns the new reference count. This value is
  /// intended to be used only for test purposes.</returns>
  IFACEMETHODIMP_(ULONG) AddRef(void);

  /// <summary>
  /// Implements <see cref="IUnknown::Release"/>.
  /// </summary>
  /// <remarks>
  /// Decrements the reference count for an interface on a COM object.
  /// </remarks>
  /// <returns>The method returns the new reference count. This value is
  /// intended to be used only for test purposes.</returns>
  IFACEMETHODIMP_(ULONG) Release(void);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::GetTitle"/>.
  /// </summary>
  /// <remarks>
  /// Gets the title text of the button or menu item that launches a
  /// specified Windows Explorer command item.
  /// </remarks>
  /// <param name="ppszName">Pointer to a buffer that, when this method
  /// returns successfully, receives the title string.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::GetIcon"/>.
  /// </summary>
  /// <remarks>
  /// Gets an icon resource string of the icon associated with the specified
  /// Windows Explorer command item.
  /// </remarks>
  /// <param name="ppszIcon">Pointer to a buffer that, when this method
  /// returns successfully, receives the resource string that identifies the
  /// icon source.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* ppszIcon);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::GetToolTip"/>.
  /// </summary>
  /// <remarks>
  /// Gets the tooltip string associated with a specified Windows Explorer
  /// command item.
  /// </remarks>
  /// <param name="ppszInfotip">Pointer to a buffer that, when this method
  /// returns successfully, receives the tooltip string.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* ppszTip);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::GetCanonicalName"/>.
  /// </summary>
  /// <remarks>
  /// Gets the GUID of a Windows Explorer command.
  /// </remarks>
  /// <param name="pguidCommandName">A pointer to a value that, when this
  /// method returns successfully, receives the command's GUID, under which
  /// it is declared in the registry.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::GetState"/>.
  /// </summary>
  /// <remarks>
  /// Gets state information associated with a specified Windows Explorer
  /// command item.
  /// </remarks>
  /// <param name="pCmdState">A pointer to a value that, when this method
  /// returns successfully, receives one or more Windows Explorer command
  /// states indicated by the <c>EXPCMDSTATE</c> constants.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE* pCmdState);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::Invoke"/>.
  /// </summary>
  /// <remarks>
  /// Invokes a Windows Explorer command.
  /// </remarks>
  /// <param name="psiItemArray">A pointer to an IShellItemArray.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP Invoke(IShellItemArray* psiItemArray, IBindCtx*);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::GetFlags"/>.
  /// </summary>
  /// <remarks>
  /// Gets the flags associated with a Windows Explorer command.
  /// </remarks>
  /// <param name="pFlags">When this method returns, this value points to the
  /// current command flags.</param>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP GetFlags(EXPCMDFLAGS* pFlags);

  /// <summary>
  /// Implements <see cref="IExplorerCommand::EnumSubCommands"/>.
  /// </summary>
  /// <remarks>
  /// Retrieves an enumerator for a command's subcommands.
  /// </remarks>
  /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it
  /// returns an <c>HRESULT</c> error code.</returns>
  IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** ppEnum);
};

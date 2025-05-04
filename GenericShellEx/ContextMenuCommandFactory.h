#pragma once

#include <vector>
#include <fstream>
#include <Unknwn.h>
#include "ContextMenuEntry.h"

/// <summary>
/// A context menu command factory.
/// </summary>
class ContextMenuCommandFactory : public IClassFactory {
  long refCount = 1;

  ContextMenuEntry contextMenuEntry;

  std::wofstream& logFile;

public:
  /// <summary>
  /// Initializes a <see cref="ContextMenuCommandFactory"/>.
  /// </summary>
  /// <param name="logFile">A log file <see cref="std::wofstream"/>.</param>
  /// <param name="contextMenuEntry">The context menu entry to present.</param>
  ContextMenuCommandFactory(std::wofstream& logFile, ContextMenuEntry contextMenuEntry);

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
  /// Implements <see cref="IClassFactory::CreateInstance"/>.
  /// </summary>
  /// <remarks>
  /// Creates an uninitialized object.
  /// </remarks>
  /// <param name="pUnkOuter">If the object is being created as part of an
  /// aggregate, specify a pointer to the controlling <see cref="IUnknown"/>
  /// interface of the aggregate. Otherwise, this parameter must be
  /// <c>NULL</c>.</param>
  /// <param name="riid">A reference to the identifier of the interface to be
  /// used to communicate with the newly created object. If <paramref
  /// name="pUnkOuter"/> is <c>NULL</c>, this parameter is generally the IID of
  /// the initializing interface; if <paramref name="pUnkOuter"/> is
  /// non-<c>NULL</c>, riid must be <see cref="IID_IUnknown"/>.</param>
  /// <param name="ppvObject">The address of pointer variable that receives the
  /// interface pointer requested in <paramref name="riid"/>. Upon successful
  /// return, <c>*</c><paramref name="ppvObject"/> contains the requested
  /// interface pointer. If the object does not support the interface specified
  /// in <paramref name="riid"/>, the implementation must set <c>*</c><paramref
  /// name="ppvObject"/> to <c>NULL</c>.</param>
  /// <returns>This method can return the standard return values <see
  /// cref="E_INVALIDARG"/>, <see cref="E_OUTOFMEMORY"/>, and <see
  /// cref="E_UNEXPECTED"/>, as well as the following values: <see
  /// cref="S_OK"/>, <see cref="CLASS_E_NOAGGREGATION"/>, and <see
  /// cref="E_NOINTERFACE"/>.</returns>
  IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject);

  /// <summary>
  /// Implements <see cref="IClassFactory::LockServer"/>.
  /// </summary>
  /// <remarks>
  /// Locks an object application open in memory. This enables instances to be
  /// created more quickly.
  /// </remarks>
  /// <returns>This method can return the standard return values <see
  /// cref="E_INVALIDARG"/>, <see cref="E_OUTOFMEMORY"/>, and <see
  /// cref="E_UNEXPECTED"/>.</returns>
  IFACEMETHODIMP LockServer(BOOL);
};

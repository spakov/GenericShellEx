#pragma once

#include <string>

/// <summary>
/// A context menu entry.
/// </summary>
struct ContextMenuEntry {
  CLSID clsid = CLSID_NULL;
  std::wstring title;
  std::wstring toolTip;
  std::wstring icon;
  std::wstring command;
};
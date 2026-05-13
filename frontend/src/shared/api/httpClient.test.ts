// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { apiDownloadBlob, downloadExternalBlob } from './httpClient';

describe('blob download helpers', () => {
  const fetchMock = vi.fn();
  const appendChild = vi.spyOn(document.body, 'appendChild');
  const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
  const createObjectUrl = vi.fn(() => 'blob:storycoffee-test');
  const revokeObjectUrl = vi.fn();

  beforeEach(() => {
    localStorage.clear();
    fetchMock.mockReset();
    appendChild.mockClear();
    click.mockClear();
    createObjectUrl.mockClear();
    revokeObjectUrl.mockClear();
    vi.stubGlobal('fetch', fetchMock);
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: createObjectUrl,
      revokeObjectURL: revokeObjectUrl,
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('downloads API blobs with bearer auth and contract method', async () => {
    localStorage.setItem('storycoffee.accessToken', 'token-1');
    fetchMock.mockResolvedValue(new Response('a,b,c', { status: 200 }));

    await apiDownloadBlob('/api/admin/logs/audit/export', 'get', '/api/admin/logs/audit/export?action=UpdatedProduct', 'audit.csv');

    expect(fetchMock).toHaveBeenCalledWith('/api/admin/logs/audit/export?action=UpdatedProduct', {
      method: 'GET',
      headers: {
        Authorization: 'Bearer token-1',
      },
    });
    expect(createObjectUrl).toHaveBeenCalled();
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:storycoffee-test');
  });

  it('downloads external blobs without bearer auth', async () => {
    localStorage.setItem('storycoffee.accessToken', 'token-1');
    fetchMock.mockResolvedValue(new Response('pdf', { status: 200 }));

    await downloadExternalBlob('https://documents.example/invoice.pdf?signature=1', 'invoice.pdf');

    expect(fetchMock).toHaveBeenCalledWith('https://documents.example/invoice.pdf?signature=1');
  });
});

/**
 * MCP integration snippets: OpenCode vs mcpServers (Claude / Cline / Continue shapes).
 * Pick a client, then JSON or YAML.
 */

export type IntegrationClientId = 'opencode' | 'claude' | 'cline' | 'continue';

export type IntegrationSerde = 'json' | 'yaml';

export const INTEGRATION_CLIENT_OPTIONS: ReadonlyArray<{
  id: IntegrationClientId;
  label: string;
  tabLabel: string;
}> = [
  { id: 'opencode', tabLabel: 'OpenCode', label: 'OpenCode' },
  { id: 'claude', tabLabel: 'Claude', label: 'Claude' },
  { id: 'cline', tabLabel: 'Cline', label: 'Cline' },
  { id: 'continue', tabLabel: 'Continue', label: 'Continue' },
];

export interface IntegrationSnippetInput {
  adapterName: string;
  gatewayBaseUrl: string;
  adapterTypeLabel: 'sse' | 'streamable-http';
  timeoutSeconds: number;
  enabled: boolean;
  headers?: Record<string, string> | null;
}

function gatewayUrl(base: string): string {
  return base.replace(/\/+$/, '');
}

function mcpEndpointUrl(input: IntegrationSnippetInput): string {
  const base = gatewayUrl(input.gatewayBaseUrl);
  const path = input.adapterTypeLabel === 'sse' ? 'sse' : 'mcp';
  return `${base}/adapters/${encodeURIComponent(input.adapterName)}/${path}`;
}

function buildClaudeEntry(input: IntegrationSnippetInput): Record<string, unknown> {
  const entry: Record<string, unknown> = {
    autoApprove: [],
    url: mcpEndpointUrl(input),
    disabled: !input.enabled,
    timeout: input.timeoutSeconds,
    type: input.adapterTypeLabel,
  };
  const h = input.headers;
  if (h && Object.keys(h).length > 0) {
    entry['headers'] = { ...h };
  }
  return entry;
}

function buildContinueRemoteEntry(input: IntegrationSnippetInput): Record<string, unknown> {
  const entry: Record<string, unknown> = {
    url: mcpEndpointUrl(input),
  };
  const h = input.headers;
  if (h && Object.keys(h).length > 0) {
    entry['headers'] = { ...h };
  }
  return entry;
}

function buildClineRemoteEntry(input: IntegrationSnippetInput): Record<string, unknown> {
  return {
    ...buildContinueRemoteEntry(input),
    disabled: !input.enabled,
  };
}

function buildOpenCodeEntry(input: IntegrationSnippetInput): Record<string, unknown> {
  const entry: Record<string, unknown> = {
    type: 'remote',
    url: mcpEndpointUrl(input),
    enabled: input.enabled,
    timeout: Math.max(1, input.timeoutSeconds) * 1000,
  };
  const h = input.headers;
  if (h && Object.keys(h).length > 0) {
    entry['headers'] = { ...h };
  }
  return entry;
}

export function valueToYaml(value: unknown, indent = 0): string {
  const pad = '  '.repeat(indent);

  if (value === null || value === undefined) {
    return 'null';
  }
  if (typeof value === 'string') {
    if (!/[\n:#]/.test(value) && !/^[\[{&*?|>'"%@`]/.test(value) && value !== '') {
      return value;
    }
    return JSON.stringify(value);
  }
  if (typeof value === 'number' || typeof value === 'boolean') {
    return JSON.stringify(value);
  }
  if (Array.isArray(value)) {
    if (value.length === 0) {
      return '[]';
    }
    const lines = value.map((v) => `${pad}- ${valueToYaml(v, indent + 1).trimStart()}`);
    return '\n' + lines.join('\n');
  }
  if (typeof value === 'object') {
    const o = value as Record<string, unknown>;
    const keys = Object.keys(o);
    if (keys.length === 0) {
      return '{}';
    }
    return (
      '\n' +
      keys
        .map((k) => {
          const v = o[k];
          const rendered = valueToYaml(v, indent + 1);
          const isComplex =
            rendered.startsWith('\n') || rendered === '[]' || rendered === '{}' || rendered.startsWith('{');
          if (isComplex) {
            return `${pad}${yamlKey(k)}:${rendered}`;
          }
          return `${pad}${yamlKey(k)}: ${rendered}`;
        })
        .join('\n')
    );
  }
  return String(value);
}

function yamlKey(k: string): string {
  if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(k)) {
    return k;
  }
  return JSON.stringify(k);
}

function objectToYaml(root: Record<string, unknown>): string {
  return valueToYaml(root, 0).replace(/^\n/, '');
}

function buildPayload(input: IntegrationSnippetInput, client: IntegrationClientId): Record<string, unknown> {
  const name = input.adapterName || 'adapter_name';
  switch (client) {
    case 'claude':
      return { mcpServers: { [name]: buildClaudeEntry({ ...input, adapterName: name }) } };
    case 'continue':
      return { mcpServers: { [name]: buildContinueRemoteEntry({ ...input, adapterName: name }) } };
    case 'cline':
      return { mcpServers: { [name]: buildClineRemoteEntry({ ...input, adapterName: name }) } };
    case 'opencode':
      return {
        $schema: 'https://opencode.ai/config.json',
        mcp: { [name]: buildOpenCodeEntry({ ...input, adapterName: name }) },
      };
    default: {
      const x: never = client;
      throw new Error(`Unknown client: ${String(x)}`);
    }
  }
}

export function buildIntegrationSnippet(
  input: IntegrationSnippetInput,
  client: IntegrationClientId,
  serde: IntegrationSerde
): string {
  const payload = buildPayload(input, client);
  if (serde === 'json') {
    return JSON.stringify(payload, null, 2);
  }
  return objectToYaml(payload);
}

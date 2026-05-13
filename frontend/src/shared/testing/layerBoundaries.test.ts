import { describe, expect, it } from 'vitest';
import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const sourceRoot = join(process.cwd(), 'src');

describe('frontend layer boundaries', () => {
  it('keeps shared independent from higher layers', () => {
    const violations = collectSourceFiles(join(sourceRoot, 'shared'))
      .flatMap((file) => collectImports(file)
        .filter((importPath) => importPath.startsWith('@/'))
        .filter((importPath) => importPath.startsWith('@/entities') || importPath.startsWith('@/features') || importPath.startsWith('@/widgets') || importPath.startsWith('@/pages') || importPath.startsWith('@/app'))
        .map((importPath) => `${relative(sourceRoot, file)} -> ${importPath}`));

    expect(violations).toEqual([]);
  });

  it('prevents lower layers from importing route pages', () => {
    const lowerLayerRoots = ['shared', 'entities', 'features', 'widgets'].map((layer) => join(sourceRoot, layer));
    const violations = lowerLayerRoots
      .flatMap(collectSourceFiles)
      .flatMap((file) => collectImports(file)
        .filter((importPath) => importPath.startsWith('@/pages'))
        .map((importPath) => `${relative(sourceRoot, file)} -> ${importPath}`));

    expect(violations).toEqual([]);
  });
});

function collectSourceFiles(directory: string): string[] {
  if (!existsSync(directory)) {
    return [];
  }

  return readdirSync(directory).flatMap((entry) => {
    const path = join(directory, entry);
    const stats = statSync(path);
    if (stats.isDirectory()) {
      return collectSourceFiles(path);
    }
    return path.endsWith('.ts') || path.endsWith('.tsx') ? [path] : [];
  });
}

function collectImports(file: string): string[] {
  const contents = readFileSync(file, 'utf8');
  return [...contents.matchAll(/from ['"]([^'"]+)['"]/g)].map((match) => match[1]);
}
